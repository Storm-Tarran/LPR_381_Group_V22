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
            tableau[0, numCols - 1] = finalZ; // keep Z consistent
        }


        private static string F(double v) => v.ToString("0.###", CultureInfo.InvariantCulture);

        private string ColLabel(int col)
        {
            int m = numRows - 1;
            int n = numCols - m - 1;
            return col < n ? $"x{col + 1}" : $"s{col - n + 1}";
        }

        private int GetBasicRow(int col)
        {
            for (int i = 1; i < numRows; i++)
            {
                if (Math.Abs(tableau[i, col] - 1.0) < 1e-9 && IsPivotColumn(i, col))
                    return i;
            }
            return -1;
        }

        private bool IsPivotColumn(int pivotRow, int col)
        {
            for (int i = 1; i < numRows; i++)
                if (i != pivotRow && Math.Abs(tableau[i, col]) > 1e-9) return false;
            return true;
        }

        private int SlackColForConstraint(int i /*1..m*/)
        {
            int m = numRows - 1;
            int n = numCols - m - 1;
            return n + (i - 1);
        }

        private double[] ShadowPrices()
        {
            int m = numRows - 1;
            var y = new double[m];
            for (int i = 1; i <= m; i++)
            {
                int sCol = SlackColForConstraint(i);
                y[i - 1] = -tableau[0, sCol];
            }
            return y;
        }

        private bool IsOptimal()
        {
            var basicSet = new HashSet<int>(basicVars);
            for (int j = 0; j < numCols - 1; j++)
            {
                if (basicSet.Contains(j)) continue; // ignore basic columns in optimality test
                if (tableau[0, j] < -1e-12) return false;
            }
            return true;
        }

        private void Pivot(int enterCol, int leaveRow)
        {
            double piv = tableau[leaveRow, enterCol];
            if (Math.Abs(piv) < EPS) throw new InvalidOperationException("Zero pivot encountered.");

            // Normalize pivot row
            for (int j = 0; j < numCols; j++) tableau[leaveRow, j] /= piv;

            // Eliminate from other rows
            for (int i = 0; i < numRows; i++)
            {
                if (i == leaveRow) continue;
                double factor = tableau[i, enterCol];
                if (Math.Abs(factor) < EPS) continue;
                for (int j = 0; j < numCols; j++)
                    tableau[i, j] -= factor * tableau[leaveRow, j];
            }

            // Update basics (basis for row = leaveRow is now enterCol)
            int idx = leaveRow - 1;
            if (idx >= 0 && idx < basicVars.Count) basicVars[idx] = enterCol;
        }

        private void ReOptimize(int maxIter = 10000)
        {
            int iter = 0;
            while (!IsOptimal())
            {
                if (iter++ > maxIter)
                    throw new InvalidOperationException("Re-optimization exceeded iteration limit.");

                // Entering: most negative reduced cost
                int enter = -1;
                double mostNeg = 0.0;
                for (int j = 0; j < numCols - 1; j++)
                {
                    double rc = tableau[0, j];
                    if (rc < mostNeg) { mostNeg = rc; enter = j; }
                }
                if (enter == -1) break;

                // Leaving: minimum ratio test on positive coefficients
                int leave = -1;
                double bestRatio = double.PositiveInfinity;
                for (int i = 1; i < numRows; i++)
                {
                    double aij = tableau[i, enter];
                    if (aij > EPS)
                    {
                        double ratio = tableau[i, numCols - 1] / aij;
                        if (ratio < bestRatio - 1e-12) { bestRatio = ratio; leave = i; }
                    }
                }
                if (leave == -1) throw new InvalidOperationException("Unbounded during re-optimization.");

                Pivot(enter, leave);
            }

            finalZ = tableau[0, numCols - 1];

            // rebuild x*
            solutionVector = new List<double>(new double[numCols - 1]);
            for (int j = 0; j < numCols - 1; j++)
            {
                int r = GetBasicRow(j);
                solutionVector[j] = (r == -1) ? 0.0 : tableau[r, numCols - 1];
            }
        }

        private void PrintTableau(string title = null)
        {
            if (!string.IsNullOrWhiteSpace(title)) Console.WriteLine($"\n=== {title} ===");
            int m = numRows - 1;
            int n = numCols - m - 1;

            // header
            var headers = new List<string>();
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

            double cbar = tableau[0, index];
            Console.WriteLine($"Reduced Cost for {ColLabel(index)}: {F(cbar)}");

            // MAX with x_j = 0 nonbasic: keep basis if c̄_j(new) >= 0.
            // Changing only c_j shifts c̄ by the same amount, so:
            // Allowable decrease = c̄_j; allowable increase = +∞.
            if (cbar > 0)
                Console.WriteLine($"Range for c{index + 1}: can DECREASE by at most {F(cbar)}, INCREASE without bound.");
            else if (Math.Abs(cbar) <= EPS)
                Console.WriteLine($"Range for c{index + 1}: at boundary (c̄=0). Any decrease makes {ColLabel(index)} enter; any increase is fine.");
            else
                Console.WriteLine("Warning: this tableau is not optimal (negative reduced cost found). Consider re-optimizing.");
        }

       
        public void ChangeNonBasicReducedCost()
        {
            Console.Write("Enter index of Non-Basic Variable to change (1-based): ");
            if (!int.TryParse(Console.ReadLine(), out int idxRaw)) { Console.WriteLine("Invalid number."); return; }
            int index = idxRaw - 1;

            Console.Write("Enter NEW reduced cost value c̄_j: ");
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
            if (r == -1)
            {
                Console.WriteLine("Error: Basic variable not found in tableau.");
                return;
            }

            // c̄_j(new) = c̄_j(old) - Δ * a_rj  (only c_Bk changed by Δ)
            double deltaLower = double.NegativeInfinity;
            double deltaUpper = double.PositiveInfinity;

            for (int j = 0; j < numCols - 1; j++)
            {
                if (j == index) continue; // basic column
                double a_rj = tableau[r, j];
                double cbar_j = tableau[0, j];

                if (a_rj > EPS)
                    deltaUpper = Math.Min(deltaUpper, cbar_j / a_rj);
                else if (a_rj < -EPS)
                    deltaLower = Math.Max(deltaLower, cbar_j / a_rj);
            }

            Console.WriteLine($"Allowable change Δ for {ColLabel(index)}’s objective coeff that keeps basis optimal: [{F(deltaLower)}, {F(deltaUpper)}]");
            Console.WriteLine("Interpretation: set c_B(new) = c_B(old) + Δ within this interval to preserve basis.");
        }

        public void ChangeBasic()
        {
            Console.Write("Enter Basic Variable index (1-based, referring to its COLUMN): ");
            if (!int.TryParse(Console.ReadLine(), out int colRaw)) { Console.WriteLine("Invalid number."); return; }
            int col = colRaw - 1;

            Console.Write("Enter NEW objective coefficient value c_B(new): ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double newVal))
            { Console.WriteLine("Invalid number."); return; }

            if (col < 0 || col >= numCols - 1 || !basicVars.Contains(col))
            {
                Console.WriteLine("Invalid index or variable is non-basic.");
                return;
            }

            int r = GetBasicRow(col);
            if (r == -1) { Console.WriteLine("Error: Basic variable not found in tableau."); return; }

            // c_B(old) = y_r with y = shadow prices, and y_r = -c̄(slack_r)
            int constraintIndex = r;
            int slackCol = SlackColForConstraint(constraintIndex);
            double y_r = -tableau[0, slackCol];
            double cBold = y_r;
            double delta = newVal - cBold;

            // Objective row update: row0(new) = row0(old) - Δ * row_r; Z(new) = Z(old) + Δ * x_B
            for (int j = 0; j < numCols; j++)
                tableau[0, j] -= delta * tableau[r, j];

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

            // x_B(new) = x_B + Δ * (kth column of B^{-1}) which is the slack_k column in final tableau rows.
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

            double oldB = tableau[k, numCols - 1];
            double delta = newB - oldB;

            int sCol = SlackColForConstraint(k);
            for (int i = 1; i < numRows; i++)
                tableau[i, numCols - 1] += delta * tableau[i, sCol];

            double[] y = ShadowPrices();
            tableau[0, numCols - 1] += y[k - 1] * delta;
            finalZ = tableau[0, numCols - 1];

            // Restore feasibility (if some RHS < 0) using dual simplex; then ensure optimality
            ResolveAll();
            PrintTableau("After RHS change (resolved)");
        }

        private void DualSimplexIfNeeded(int maxIter = 10000)
        {
            int iter = 0;
            while (true)
            {
                int leave = -1;
                double mostNeg = 0.0;
                for (int i = 1; i < numRows; i++)
                {
                    double bi = tableau[i, numCols - 1];
                    if (bi < mostNeg - 1e-12) { mostNeg = bi; leave = i; }
                }
                if (leave == -1) break; // feasible

                if (iter++ > maxIter)
                    throw new InvalidOperationException("Dual simplex exceeded iteration limit.");

                int enter = -1;
                double bestRatio = double.PositiveInfinity;
                for (int j = 0; j < numCols - 1; j++)
                {
                    double aij = tableau[leave, j];
                    if (aij < -EPS)
                    {
                        double ratio = tableau[0, j] / (-aij);
                        if (ratio < bestRatio - 1e-12) { bestRatio = ratio; enter = j; }
                    }
                }
                if (enter == -1) throw new InvalidOperationException("Infeasible after RHS change (no entering col).");

                Pivot(enter, leave);
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
            double[] y = ShadowPrices();
            double yi = y[row - 1];

            double deltaLower = double.NegativeInfinity, deltaUpper = double.PositiveInfinity;
            if (yi > EPS) deltaUpper = Math.Min(deltaUpper, cbar / yi);
            else if (yi < -EPS) deltaLower = Math.Max(deltaLower, cbar / yi);

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

            double[] y = ShadowPrices();
            double yi = y[row - 1];
            tableau[0, col] -= yi * delta;

            ResolveAll();
            PrintTableau("After a_ij change (resolved)");
        }

      

        public void AddNewActivity()
        {
            Console.Write("Enter objective coefficient for new variable c_new (e.g., 5): ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double cNew))
            { Console.WriteLine("Invalid number."); return; }

            int m = numRows - 1;                 // #constraints
            int n = numCols - m - 1;             // #decision vars (current)

            Console.WriteLine("Enter technological coefficients a_i for each constraint row:");
            double[] aNew = new double[m];
            for (int i = 1; i <= m; i++)
            {
                Console.Write($"  a[{i}] = ");
                if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                { Console.WriteLine("Invalid number."); return; }
                aNew[i - 1] = v;
            }

            // Compute reduced cost: c̄_new = c_new - y^T a_new
            var y = ShadowPrices();
            double yTa = 0.0;
            for (int i = 0; i < m; i++) yTa += y[i] * aNew[i];
            double cbarNew = cNew - yTa;

            // Insert new decision column at index n, shifting slacks to the right (RHS/Z stays last)
            int oldCols = numCols;
            int newCols = oldCols + 1;
            var newT = new double[numRows, newCols];

            // Copy existing decision columns [0 .. n-1]
            for (int i = 0; i < numRows; i++)
                for (int j = 0; j < n; j++)
                    newT[i, j] = tableau[i, j];

            // Insert NEW decision column at j = n
            newT[0, n] = cbarNew;          // reduced cost
            for (int i = 1; i < numRows; i++)
                newT[i, n] = aNew[i - 1];

            // Copy old slack block j = n .. oldCols-2 into j = n+1 .. newCols-2
            for (int i = 0; i < numRows; i++)
                for (int j = n; j < oldCols - 1; j++)
                    newT[i, j + 1] = tableau[i, j];

            // Copy RHS/Z to the last column
            for (int i = 0; i < numRows; i++)
                newT[i, newCols - 1] = tableau[i, oldCols - 1];

            // Commit
            tableau = newT;
            numCols = newCols;

           
            for (int i = 0; i < basicVars.Count; i++)
            {
                if (basicVars[i] >= n) basicVars[i] += 1;
            }

            Console.WriteLine($"Added new variable x{n + 1}: c = {cNew}, y^T a = {yTa:0.###}, c̄ = {cbarNew:0.###}. Re-optimizing if needed...");

            Console.WriteLine($"Added new variable x{n + 1}: c = {cNew}, y^T a = {yTa:0.###}, c̄ = {cbarNew:0.###}. Resolving...");
            ResolveAll();
            PrintTableau("After adding variable (resolved)");
        
        }

        public void AddNewConstraint()
        {
            Console.WriteLine("Enter technological coefficients for existing columns (excluding RHS):");
            double[] tech = new double[numCols - 1];
            for (int j = 0; j < numCols - 1; j++)
            {
                Console.Write($"  col {ColLabel(j)} = ");
                if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double v))
                { Console.WriteLine("Invalid number."); return; }
                tech[j] = v;
            }

            Console.Write("Enter RHS value: ");
            if (!double.TryParse(Console.ReadLine(), NumberStyles.Float, CultureInfo.InvariantCulture, out double rhs))
            { Console.WriteLine("Invalid number."); return; }

            // Expand by 1 row and 1 slack column (insert before RHS), push RHS/Z right
            var newT = new double[numRows + 1, numCols + 1];

            // copy old
            for (int i = 0; i < numRows; i++)
            {
                for (int j = 0; j < numCols - 1; j++) newT[i, j] = tableau[i, j];
                newT[i, numCols] = tableau[i, numCols - 1];
            }

            // new row tech
            for (int j = 0; j < numCols - 1; j++) newT[numRows, j] = tech[j];

            // new slack column (before RHS/Z): 1 in new row, 0 elsewhere
            for (int i = 0; i <= numRows; i++) newT[i, numCols - 1] = (i == numRows) ? 1.0 : 0.0;
            newT[numRows, numCols] = rhs;  // RHS
            newT[0, numCols - 1] = 0.0;    // reduced cost of the new slack

            tableau = newT;
            numRows++;
            numCols++;
            basicVars.Add(numCols - 2); // new slack basic in new row

            Console.WriteLine($"Added new constraint (row {numRows - 1}). Resolving...");
            ResolveAll();
            PrintTableau("After adding constraint (resolved)");
        }

        

        public void DisplayShadowPrices()
        {
            Console.WriteLine("Shadow Prices (y = -c̄_slack):");
            var y = ShadowPrices();
            for (int i = 0; i < y.Length; i++)
                Console.WriteLine($"  Constraint {i + 1}: y_{i + 1} = {F(y[i])}");
        }

        public void PerformDuality()
        {
            int m = numRows - 1;
            int n = numCols - m - 1;

            double[] b = new double[m];
            for (int i = 1; i <= m; i++) b[i - 1] = tableau[i, numCols - 1];

            // Careful: rows in final tableau are B^{-1}A, not A. This is for display only.
            double[,] Atilde = new double[m, n];
            for (int i = 1; i <= m; i++)
                for (int j = 0; j < n; j++)
                    Atilde[i - 1, j] = tableau[i, j];

            var y = ShadowPrices();

            // implied c = c̄ + A^T y  (using Atilde for display)
            double[] c = new double[n];
            for (int j = 0; j < n; j++)
            {
                double ATy = 0.0;
                for (int i = 0; i < m; i++) ATy += Atilde[i, j] * y[i];
                c[j] = tableau[0, j] + ATy;
            }

            Console.WriteLine("Dual (display form — coefficients derived from final tableau):");
            Console.WriteLine($"  minimize b^T y,  with y >= 0,  A^T y >= c");
            Console.WriteLine($"  b = [{string.Join(", ", b.Select(F))}]");
            Console.WriteLine($"  y* = [{string.Join(", ", y.Select(F))}]");
            Console.WriteLine("  Constraints per primal var j: (A^T y)_j >= c_j (display coefficients use B^{-1}A)");
            for (int j = 0; j < n; j++)
            {
                var lhs = new List<string>();
                for (int i = 0; i < m; i++) lhs.Add($"{F(Atilde[i, j])}*y{i + 1}");
                Console.WriteLine($"    j={j + 1}: {string.Join(" + ", lhs)} >= {F(c[j])}");
            }

            double dualValue = 0.0;
            for (int i = 0; i < m; i++) dualValue += b[i] * y[i];
            Console.WriteLine($"  Primal Z* = {F(tableau[0, numCols - 1])}, Dual b^T y = {F(dualValue)}");
            Console.WriteLine(Math.Abs(tableau[0, numCols - 1] - dualValue) <= 1e-6
                ? "  Strong Duality holds."
                : "  Warning: values differ (numerical issues or non-optimal tableau).");
        }

        

        public double[,] CurrentTableau => (double[,])tableau.Clone();
        public double CurrentZ => finalZ;
        public IReadOnlyList<double> CurrentSolutionVector => solutionVector.AsReadOnly();

       
        public void Recalculate()
        {
            DualSimplexIfNeeded();    // fix feasibility if any RHS < 0
            if (!IsOptimal()) ReOptimize();
            Console.WriteLine("\n=== Recalculate() complete (feasibility & optimality restored) ===");
        }

        private void RebuildBasicsFromTableau()
        {
            int m = numRows - 1;
            if (basicVars.Count != m)
            {
                basicVars.Clear();
                for (int i = 0; i < m; i++) basicVars.Add(-1);
            }

            for (int i = 1; i <= m; i++)
            {
                int pivotCol = -1;
                for (int j = 0; j < numCols - 1; j++)
                {
                    if (Math.Abs(tableau[i, j] - 1.0) < EPS && IsPivotColumn(i, j))
                    {
                        pivotCol = j;
                        break;
                    }
                }
                if (pivotCol != -1) basicVars[i - 1] = pivotCol;
            }
        }

        // One-button restore: fix basis from tableau, then restore feasibility and optimality,
        // and refresh Z and x*.
        private void ResolveAll()
        {
            RebuildBasicsFromTableau();
            DualSimplexIfNeeded();
            ReOptimize(); // ReOptimize also refreshes finalZ & solutionVector
        }

        private static void ClearAndShowOptimal(SensitivityAnalyzer sa, string header = null)
        {
            Console.Clear();
            if (!string.IsNullOrWhiteSpace(header))
            {
                Console.WriteLine(header);
                Console.WriteLine();
            }

            PrintOptimalTable(sa);

        }

        private static void PrintOptimalTable(SensitivityAnalyzer sa, int colWidth = 12)
        {
            var T = sa.CurrentTableau;
            if (T == null)
            {
                Console.WriteLine("(No tableau available.)");
                return;
            }

            int rows = T.GetLength(0);
            int cols = T.GetLength(1);
            if (cols <= 0 || rows <= 0)
            {
                Console.WriteLine("(Empty tableau.)");
                return;
            }

            // Header
            Console.Write("".PadLeft(6));
            for (int j = 0; j < cols; j++)
            {
                string name = (j == cols - 1) ? "RHS" : $"c{j + 1}";
                Console.Write($"{name}".PadLeft(colWidth));
            }
            Console.WriteLine();

            // Rows: row 0 is Z, others are constraints
            for (int i = 0; i < rows; i++)
            {
                string rowLabel = (i == 0) ? "Z" : $"r{i}";
                Console.Write($"{rowLabel}".PadLeft(6));

                for (int j = 0; j < cols; j++)
                {
                    double v = T[i, j];
                    Console.Write(string.Format(CultureInfo.InvariantCulture,
    "{0," + colWidth + ":0.###}", v));
                }
                Console.WriteLine();
            }

            Console.WriteLine();
        }

        private static void Pause()
        {
            Console.WriteLine("\nPress Enter to continue...");
            Console.ReadLine();
        }

    }
}
