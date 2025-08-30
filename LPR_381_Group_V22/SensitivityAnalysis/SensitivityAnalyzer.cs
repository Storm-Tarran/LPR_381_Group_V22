using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LPR_381_Group_V22.SensitivityAnalysis
{
    internal class SensitivityAnalyzer
    {
       
        private double[,] tableau;
        private List<double> solutionVector; 
        private double finalZ;               
        private int numRows;
        private int numCols;

       
        private readonly List<int> basicVars;

        private const double EPS = 1e-9;

        public SensitivityAnalyzer(double[,] finalTableau, List<double> solution, double zValue, List<int> basicVariables)
        {
            tableau = (double[,])finalTableau.Clone();
            solutionVector = new List<double>(solution);
            finalZ = zValue;
            numRows = tableau.GetLength(0);
            numCols = tableau.GetLength(1);
            basicVars = new List<int>(basicVariables);

            // Ensure Z on RHS row 0 is set
            tableau[0, numCols - 1] = finalZ;

            // Basis might be stale, rebuild it from the tableau
            RebuildBasicsFromTableau();

            // Optional: warn if your first few variables are supposed to be binary
            ValidateBinaryConstraints();
        }

        

        private void ValidateBinaryConstraints()
        {
            
            for (int i = 0; i < Math.Min(solutionVector.Count, 6); i++)
            {
                if (Math.Abs(solutionVector[i] - Math.Round(solutionVector[i])) > EPS)
                    Console.WriteLine($"Warning: x{i + 1} = {solutionVector[i]:0.###} violates binary constraint.");
            }
        }

        private string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private string ColLabel(int col)
        {
            int m = numRows - 1;
            int n = numCols - m - 1;
            return col < n ? $"x{col + 1}" : $"s{col - n + 1}";
        }

        private int SlackColForConstraint(int i /*1..m*/)
        {
            int m = numRows - 1;
            int n = numCols - m - 1;
            return n + (i - 1);
        }

        public int GetBasicRow(int col)
        {
            for (int i = 1; i < numRows; i++)
            {
                if (Math.Abs(tableau[i, col] - 1.0) < EPS && IsPivotColumn(i, col))
                    return i;
            }
            return -1;
        }

        private bool IsPivotColumn(int pivotRow, int col)
        {
            for (int i = 1; i < numRows; i++)
                if (i != pivotRow && Math.Abs(tableau[i, col]) > EPS) return false;
            return true;
        }

        private bool IsOptimal()
        {
            // With Z−C stored, for a maximization model: optimal if all NON-BASIC reduced costs >= 0
            var basicSet = new HashSet<int>(basicVars);
            for (int j = 0; j < numCols - 1; j++)
            {
                if (basicSet.Contains(j)) continue;
                if (tableau[0, j] < -EPS) return false;
            }
            return true;
        }

        private void Pivot(int enterCol, int leaveRow)
        {
            double piv = tableau[leaveRow, enterCol];
            if (Math.Abs(piv) < EPS) throw new InvalidOperationException("Zero pivot encountered.");

            // normalize pivot row
            for (int j = 0; j < numCols; j++) tableau[leaveRow, j] /= piv;

            // eliminate column in all other rows (including Z-row)
            for (int i = 0; i < numRows; i++)
            {
                if (i == leaveRow) continue;
                double factor = tableau[i, enterCol];
                if (Math.Abs(factor) < EPS) continue;
                for (int j = 0; j < numCols; j++)
                    tableau[i, j] -= factor * tableau[leaveRow, j];
            }

            // update basis record
            int idx = leaveRow - 1;
            if (idx >= 0 && idx < basicVars.Count) basicVars[idx] = enterCol;
        }

        private void ReOptimize(int maxIter = 10000)
        {
            int iter = 0;
            while (!IsOptimal())
            {
                if (iter++ > maxIter) throw new InvalidOperationException("Re-optimization exceeded iteration limit.");

                // choose entering column: most negative reduced cost (Z−C)
                int enter = -1;
                double mostNeg = 0.0;
                for (int j = 0; j < numCols - 1; j++)
                {
                    if (basicVars.Contains(j)) continue;
                    double rc = tableau[0, j];
                    if (rc < mostNeg) { mostNeg = rc; enter = j; }
                }
                if (enter == -1) break;

                // leaving by min ratio
                int leave = -1;
                double bestRatio = double.PositiveInfinity;
                for (int i = 1; i < numRows; i++)
                {
                    double aij = tableau[i, enter];
                    if (aij > EPS)
                    {
                        double ratio = tableau[i, numCols - 1] / aij;
                        if (ratio < bestRatio - EPS) { bestRatio = ratio; leave = i; }
                    }
                }
                if (leave == -1) throw new InvalidOperationException("Unbounded during re-optimization.");

                Pivot(enter, leave);
            }

            finalZ = tableau[0, numCols - 1];

            // rebuild primal solution from basis
            solutionVector = new List<double>(new double[numCols - 1]);
            for (int j = 0; j < numCols - 1; j++)
            {
                int r = GetBasicRow(j);
                solutionVector[j] = (r == -1) ? 0.0 : tableau[r, numCols - 1];
            }
            ValidateBinaryConstraints();
        }

        private void DualSimplexIfNeeded(int maxIter = 10000)
        {
            // If any RHS is negative, do dual simplex until all RHS >= 0
            int iter = 0;
            while (true)
            {
                int leave = -1;
                double mostNeg = 0.0;
                for (int i = 1; i < numRows; i++)
                {
                    double bi = tableau[i, numCols - 1];
                    if (bi < mostNeg - EPS) { mostNeg = bi; leave = i; }
                }
                if (leave == -1) break; // all RHS >= 0

                if (iter++ > maxIter) throw new InvalidOperationException("Dual simplex exceeded iteration limit.");

                // choose enter: a_ij < 0 and minimize (Z−C)_j / (-a_ij)
                int enter = -1;
                double bestRatio = double.PositiveInfinity;
                for (int j = 0; j < numCols - 1; j++)
                {
                    double aij = tableau[leave, j];
                    if (aij < -EPS)
                    {
                        double ratio = tableau[0, j] / (-aij);
                        if (ratio < bestRatio - EPS) { bestRatio = ratio; enter = j; }
                    }
                }
                if (enter == -1) throw new InvalidOperationException("Infeasible after RHS change (dual simplex).");

                Pivot(enter, leave);
            }
        }

        private void ResolveAll()
        {
            RebuildBasicsFromTableau();
            DualSimplexIfNeeded();
            ReOptimize();
        }

        

        private double[] ShadowPrices()
        {
            int m = numRows - 1;
            var y = new double[m];
            for (int i = 1; i <= m; i++)
            {
                int sCol = SlackColForConstraint(i);
                y[i - 1] = tableau[0, sCol]; 
            }
            return y;
        }

        
        
        private double[] RecoverObjectiveC()
        {
            int m = numRows - 1;
            int n = numCols - m - 1;
            var y = ShadowPrices();

            // Ã = B^{-1}A decision-part
            double[,] Atilde = new double[m, n];
            for (int i = 1; i <= m; i++)
                for (int j = 0; j < n; j++)
                    Atilde[i - 1, j] = tableau[i, j];

            var c = new double[n];
            for (int j = 0; j < n; j++)
            {
                double ATy = 0.0;
                for (int i = 0; i < m; i++) ATy += Atilde[i, j] * y[i];
                c[j] = ATy - tableau[0, j]; // (A^T y) − (Z−C)
            }
            return c;
        }

       

        public void PrintTableau(string title = null)
        {
            if (!string.IsNullOrWhiteSpace(title)) Console.WriteLine($"\n=== {title} ===");

            var headers = new List<string>();
            int m = numRows - 1;
            int n = numCols - m - 1;
            for (int j = 0; j < n; j++) headers.Add(ColLabel(j));
            for (int j = n; j < n + m; j++) headers.Add(ColLabel(j));
            headers.Add("RHS/Z");
            Console.WriteLine(string.Join("\t", headers));

            for (int i = 0; i < numRows; i++)
            {
                var row = new List<string>();
                for (int j = 0; j < numCols; j++) row.Add(F(tableau[i, j]));
                Console.WriteLine(string.Join("\t", row));
            }
            Console.WriteLine($"Current Solution: Z = {F(finalZ)}");
            for (int j = 0; j < numCols - 1; j++)
                Console.WriteLine($"{ColLabel(j)} = {F(solutionVector[j])}");
        }

       

        
        public void DisplayRangeNonBasic()
        {
            Console.Write("Enter Non-Basic Variable index (1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int indexRaw)) { Console.WriteLine("Invalid number."); return; }
            int index = indexRaw - 1;

            if (index < 0 || index >= numCols - 1 || basicVars.Contains(index))
            {
                Console.WriteLine("Invalid index or variable is basic.");
                return;
            }

            double cbar = tableau[0, index]; // Z−C
            Console.WriteLine($"Reduced Cost for {ColLabel(index)}: {F(cbar)}");

            if (cbar > EPS)
                Console.WriteLine($"Range for c{index + 1}: can DECREASE by at most {F(cbar)}, INCREASE without bound.");
            else if (Math.Abs(cbar) <= EPS)
                Console.WriteLine($"Range for c{index + 1}: at boundary (c̄=0). Any decrease makes {ColLabel(index)} enter; any increase is fine.");
            else
                Console.WriteLine("Warning: tableau not optimal (negative reduced cost found). Consider re-optimizing.");
        }

        
        public void ChangeNonBasicReducedCost()
        {
            Console.Write("Enter index of Non-Basic Variable to change (1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int idxRaw)) { Console.WriteLine("Invalid number."); return; }
            int index = idxRaw - 1;

            Console.Write("Enter NEW reduced cost value c̄_j (Z−C): ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double newCbar))
            { Console.WriteLine("Invalid number."); return; }

            if (index < 0 || index >= numCols - 1 || basicVars.Contains(index))
            {
                Console.WriteLine("Invalid index or variable is basic.");
                return;
            }

            tableau[0, index] = newCbar;
            Console.WriteLine($"Updated reduced cost for {ColLabel(index)}: c̄ = {F(newCbar)}");

            ResolveAll();
            PrintTableau("After nonbasic c̄ change (resolved)");
        }

       
        public void DisplayRangeBasic()
        {
            Console.Write("Enter Basic Variable index (1-based, referring to its COLUMN): ");
            if (!int.TryParse(Console.ReadLine(), out int indexRaw)) { Console.WriteLine("Invalid number."); return; }
            int index = indexRaw - 1;

            if (index < 0 || index >= numCols - 1 || !basicVars.Contains(index))
            {
                Console.WriteLine("Invalid index or variable is non-basic.");
                return;
            }

            int r = GetBasicRow(index);
            if (r == -1) { Console.WriteLine("Error: Basic variable not found in tableau."); return; }

            
            double deltaLower = double.NegativeInfinity;
            double deltaUpper = double.PositiveInfinity;

            for (int j = 0; j < numCols - 1; j++)
            {
                if (j == index || basicVars.Contains(j)) continue;
                double a_rj = tableau[r, j];
                double cbar_j = tableau[0, j];

                if (a_rj > EPS)
                   
                    deltaLower = Math.Max(deltaLower, -cbar_j / a_rj);
                else if (a_rj < -EPS)
                    
                    deltaUpper = Math.Min(deltaUpper, -cbar_j / a_rj);
            }

            Console.WriteLine($"Allowable change Δ for {ColLabel(index)}’s objective coeff that keeps basis optimal: [{F(deltaLower)}, {F(deltaUpper)}]");
            Console.WriteLine("Interpretation: set c_B(new) = c_B(old) + Δ within this interval to preserve basis.");
        }

       
        public void ChangeBasic()
        {
            Console.Write("Enter Basic Variable index (1-based, referring to its COLUMN): ");
            if (!int.TryParse(Console.ReadLine(), out int colRaw)) { Console.WriteLine("Invalid number."); return; }
            int col = colRaw - 1;

            Console.Write("Enter change Δ for objective coefficient (c_B(new) = c_B(old) + Δ): ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double delta))
            { Console.WriteLine("Invalid number."); return; }

            if (col < 0 || col >= numCols - 1 || !basicVars.Contains(col))
            {
                Console.WriteLine("Invalid index or variable is non-basic.");
                return;
            }

            int r = GetBasicRow(col);
            if (r == -1) { Console.WriteLine("Error: Basic variable not found in tableau."); return; }

            
            for (int j = 0; j < numCols - 1; j++)
                tableau[0, j] += delta * tableau[r, j];

            
            tableau[0, numCols - 1] += delta * tableau[r, numCols - 1];
            finalZ = tableau[0, numCols - 1];

            Console.WriteLine($"Applied Δ = {F(delta)} to c_B for {ColLabel(col)}. Objective row updated; Z adjusted by Δ*x_B.");

            ResolveAll();
            PrintTableau("After c_B change (resolved)");
        }

        
        public void DisplayRangeRHS()
        {
            Console.Write("Enter constraint index (1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int k) || k < 1 || k >= numRows)
            { Console.WriteLine("Invalid constraint index."); return; }

            int sCol = SlackColForConstraint(k);
            double[] y = ShadowPrices();
            double shadowPrice = y[k - 1];

            double deltaLower = double.NegativeInfinity;
            double deltaUpper = double.PositiveInfinity;

            for (int i = 1; i < numRows; i++)
            {
                double coeff = tableau[i, sCol];
                double bi = tableau[i, numCols - 1];

                if (coeff > EPS)
                    deltaLower = Math.Max(deltaLower, -bi / coeff);     
                else if (coeff < -EPS)
                    deltaUpper = Math.Min(deltaUpper, -bi / coeff);     
            }

            double currentRHS = tableau[k, numCols - 1];
            Console.WriteLine($"Shadow Price y_{k} = {F(shadowPrice)}");
            Console.WriteLine($"Allowable RHS change Δ for constraint {k}: [{F(deltaLower)}, {F(deltaUpper)}]");
            Console.WriteLine($"So b_{k} may vary within [{F(currentRHS + deltaLower)}, {F(currentRHS + deltaUpper)}] without changing the basis.");
        }

        
        public void ChangeRHS()
        {
            Console.Write("Enter constraint index to change RHS (1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int k) || k < 1 || k >= numRows)
            { Console.WriteLine("Invalid constraint index."); return; }

            Console.Write("Enter NEW RHS value: ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double newB))
            { Console.WriteLine("Invalid number."); return; }

            
            var snap = (double[,])tableau.Clone();
            var basicsSnap = new List<int>(basicVars);
            double oldZ = finalZ;

            double oldB = tableau[k, numCols - 1];
            double delta = newB - oldB;

            int sCol = SlackColForConstraint(k);

            
            for (int i = 1; i < numRows; i++)
                tableau[i, numCols - 1] += delta * tableau[i, sCol];

            
            double[] y = ShadowPrices();
            tableau[0, numCols - 1] += y[k - 1] * delta;
            finalZ = tableau[0, numCols - 1];

            try
            {
                DualSimplexIfNeeded();
                ReOptimize();
                PrintTableau("After RHS change (resolved)");
            }
            catch
            {
                tableau = snap;
                finalZ = oldZ;
                basicVars.Clear(); basicVars.AddRange(basicsSnap);
                Console.WriteLine("This RHS change makes the model infeasible for the current basis. "
                                  + "Use option 5 (RHS range) to see the allowable interval.");
            }
        }

     
        public void DisplayRangeNonBasicColumn()
        {
            Console.Write("Enter row (constraint, 1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int row) || row < 1 || row >= numRows)
            { Console.WriteLine("Invalid row."); return; }

            Console.Write("Enter column (non-basic variable, 1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int colRaw)) { Console.WriteLine("Invalid col."); return; }
            int col = colRaw - 1;

            if (col < 0 || col >= numCols - 1 || basicVars.Contains(col))
            {
                Console.WriteLine("Invalid column, or column is basic.");
                return;
            }

            double aij = tableau[row, col];
            double cbar = tableau[0, col];      
            double yi = ShadowPrices()[row - 1];

            
            double deltaLower = double.NegativeInfinity, deltaUpper = double.PositiveInfinity;
            if (yi > EPS) deltaLower = Math.Max(deltaLower, -cbar / yi);
            else if (yi < -EPS) deltaUpper = Math.Min(deltaUpper, -cbar / yi);

            Console.WriteLine($"Allowable Δ for a[{row},{ColLabel(col)}] keeping basis optimal: [{F(deltaLower)}, {F(deltaUpper)}]");
            Console.WriteLine($"So a[{row},{ColLabel(col)}] may vary within [{F(aij + deltaLower)}, {F(aij + deltaUpper)}].");
        }

        public void ChangeNonBasicColumn()
        {
            Console.Write("Enter row (constraint, 1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int row) || row < 1 || row >= numRows)
            { Console.WriteLine("Invalid row."); return; }

            Console.Write("Enter column (non-basic variable, 1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int colRaw)) { Console.WriteLine("Invalid col."); return; }
            int col = colRaw - 1;

            Console.Write("Enter NEW a_ij: ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double newVal))
            { Console.WriteLine("Invalid number."); return; }

            if (col < 0 || col >= numCols - 1 || basicVars.Contains(col))
            {
                Console.WriteLine("Invalid column, or column is basic.");
                return;
            }

            double oldVal = tableau[row, col];
            double delta = newVal - oldVal;
            tableau[row, col] = newVal;

            double yi = ShadowPrices()[row - 1];
            tableau[0, col] += yi * delta;        

            ResolveAll();
            PrintTableau("After a_ij change (resolved)");
        }

        
        public void AddNewActivity()
        {
            Console.Write("Enter objective coefficient for new variable c_new (e.g., 5): ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double cNew))
            { Console.WriteLine("Invalid number."); return; }

            int m = numRows - 1;
            int n = numCols - m - 1;

            Console.WriteLine("Enter technological coefficients a_i for each constraint row:");
            double[] aNew = new double[m];
            for (int i = 1; i <= m; i++)
            {
                Console.Write($"  a[{i}] = ");
                if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                { Console.WriteLine("Invalid number."); return; }
                aNew[i - 1] = v;
            }

            double[] y = ShadowPrices();
            double yTa = 0.0; for (int i = 0; i < m; i++) yTa += y[i] * aNew[i];
            double cbarNew = yTa - cNew; 

            
            var newT = new double[numRows, numCols + 1];
            for (int i = 0; i < numRows; i++)
            {
                
                for (int j = 0; j < n; j++) newT[i, j] = tableau[i, j];

                
                newT[i, n] = (i == 0) ? cbarNew : aNew[i - 1];

                
                for (int j = n; j < numCols - 1; j++) newT[i, j + 1] = tableau[i, j];

                
                newT[i, numCols] = tableau[i, numCols - 1];
            }

            tableau = newT;
            numCols++;

           
            for (int i = 0; i < basicVars.Count; i++)
                if (basicVars[i] >= n) basicVars[i]++;

            Console.WriteLine($"Added new variable x{n + 1}: c = {F(cNew)}, y^T a = {F(yTa)}, c̄ = {F(cbarNew)}. Resolving...");
            ResolveAll();
            PrintTableau("After adding variable (resolved)");
        }

        
        public void AddNewConstraint()
        {
            int oldM = numRows - 1;
            int oldNPlusM = numCols - 1;

            Console.WriteLine("Enter technological coefficients for existing columns (excluding RHS):");
            double[] tech = new double[oldNPlusM];
            for (int j = 0; j < oldNPlusM; j++)
            {
                Console.Write($"  col {ColLabel(j)} = ");
                if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                { Console.WriteLine("Invalid number."); return; }
                tech[j] = v;
            }

            Console.Write("Enter RHS value: ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double rhs))
            { Console.WriteLine("Invalid number."); return; }

            AddNewConstraintNonInteractive(tech, rhs);
        }

        public void AddNewConstraintNonInteractive(double[] tech, double rhs)
        {
            int oldM = numRows - 1;
            int oldNPlusM = numCols - 1;

            // Build new tableau with an extra slack column and one extra row
            var newT = new double[numRows + 1, numCols + 1];

            // copy existing
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols - 1; j++) newT[i, j] = tableau[i, j];
                newT[i, numCols] = tableau[i, numCols - 1]; // RHS
            }

            // new slack column is identity in the new row
            int newSlackCol = numCols - 1;
            for (int i = 0; i < numRows + 1; i++)
                newT[i, newSlackCol] = (i == numRows) ? 1.0 : 0.0;

            
            for (int j = 0; j < oldNPlusM; j++)
            {
                double coeff = -tech[j];
                for (int pos = 0; pos < oldM; pos++)
                {
                    int basicCol = basicVars[pos];
                    coeff += tech[basicCol] * tableau[pos + 1, j];
                }
                newT[numRows, j] = coeff;
            }

            
            double aX = 0.0;
            for (int j = 0; j < Math.Min(tech.Length, solutionVector.Count); j++)
                aX += tech[j] * solutionVector[j];

            newT[numRows, numCols] = rhs - aX;

            
            newT[0, newSlackCol] = 0.0;

            tableau = newT;
            numRows++;
            numCols++;
            basicVars.Add(newSlackCol);

            Console.WriteLine($"Added new constraint (row {numRows - 1}). Resolving...");
            ResolveAll();
            PrintTableau("After adding constraint (resolved)");
        }

       
        public void DisplayShadowPrices()
        {
            Console.WriteLine("Shadow Prices y (Z−C on slack columns):");
            var y = ShadowPrices();
            for (int i = 0; i < y.Length; i++)
                Console.WriteLine($"  Constraint {i + 1}: y_{i + 1} = {F(y[i])}");
        }

        
        public void PerformDuality()
        {
            int m = numRows - 1;
            int n = numCols - m - 1;

            
            double[] bPrime = new double[m];
            for (int i = 1; i <= m; i++) bPrime[i - 1] = tableau[i, numCols - 1];

            
            double[,] Atilde = new double[m, n];
            for (int i = 1; i <= m; i++)
                for (int j = 0; j < n; j++)
                    Atilde[i - 1, j] = tableau[i, j];

            var y = ShadowPrices();

            
            double[] chat = new double[n];
            for (int j = 0; j < n; j++)
            {
                double ATy = 0.0; for (int i = 0; i < m; i++) ATy += Atilde[i, j] * y[i];
                chat[j] = ATy - tableau[0, j];
            }

            Console.WriteLine("Dual (derived from final tableau; tableau stores Z−C):");
            Console.WriteLine("  For max with ≤-type rows: minimize b^T y, s.t. A^T y ≥ c, y ≥ 0.");
            Console.WriteLine($"  y* = [{string.Join(", ", y.Select(F))}]");
            Console.WriteLine($"  ĉ (consistent with tableau) = [{string.Join(", ", chat.Select(F))}]");
            Console.WriteLine($"  Z* (from tableau) = {F(tableau[0, numCols - 1])}");
            Console.WriteLine("  Note: b here equals B^{-1}b (tableau RHS), so we do not compare b^T y to Z* numerically.");
        }

       

        private void RebuildBasicsFromTableau()
        {
            int m = numRows - 1;
            basicVars.Clear();
            for (int i = 0; i < m; i++) basicVars.Add(-1);

            for (int i = 1; i <= m; i++)
            {
                for (int j = 0; j < numCols - 1; j++)
                {
                    if (Math.Abs(tableau[i, j] - 1.0) < EPS && IsPivotColumn(i, j))
                    {
                        basicVars[i - 1] = j;
                        break;
                    }
                }
            }
        }

       

        public double[,] CurrentTableau => (double[,])tableau.Clone();
        public double CurrentZ => finalZ;
        public IReadOnlyList<double> CurrentSolutionVector => solutionVector.AsReadOnly();
    }
}
