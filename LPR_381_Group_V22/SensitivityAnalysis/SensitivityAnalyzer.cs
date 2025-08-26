using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_381_Group_V22.SensitivityAnalysis
{
    internal class SensitivityAnalyzer
    {
        private double[,] tableau;
        private List<double> solutionVector;
        private double finalZ;
        private int numRows;
        private int numCols;
        private List<int> basicVars;

        public SensitivityAnalyzer(double[,] finalTableau, List<double> solution, double zValue, List<int> basicVariables)
        {
            tableau = (double[,])finalTableau.Clone();
            solutionVector = new List<double>(solution);
            finalZ = zValue;
            numRows = tableau.GetLength(0);
            numCols = tableau.GetLength(1);
            basicVars = new List<int>(basicVariables);
        }
        public void DisplayRangeNonBasic()
        {

            Console.Write("Enter Non-Basic Variable index (1-based):: ");
            int index = int.Parse(Console.ReadLine()) - 1;

            if (index < 0 || index >= numCols - 1 || basicVars.Contains(index))
            {
                Console.WriteLine("Invalid index or variable is basic.");
                return;
            }

            double reducedCost = tableau[0, index];
            Console.WriteLine($"Reduced Cost for x{index + 1}: {reducedCost:F3}");

            if (reducedCost > 0)
            {
                Console.WriteLine($"Range for c{index + 1}: Can decrease by up to {reducedCost:F3}, increase indefinitely.");
            }
            else if (reducedCost < 0) // Reduced cost < 0 means coefficient can increase
            {
                Console.WriteLine($"Range for c{index + 1}: Can increase by up to {-reducedCost:F3}, decrease indefinitely.");
            }
            else
            {
                Console.WriteLine($"Range for c{index + 1}: Coefficient is at boundary; any change may affect optimality.");
            }

        }
        public void ChangeNonBasic()
        {

            Console.Write("Enter index of Non-Basic Variable to change (1-based): ");
            int index = int.Parse(Console.ReadLine()) - 1;
            Console.Write("Enter new value: ");
            double val = double.Parse(Console.ReadLine());

            if (index < 0 || index >= numCols - 1 || basicVars.Contains(index))
            {
                Console.WriteLine("Invalid index or variable is basic.");
                return;
            }

            tableau[0, index] = val;
            Console.WriteLine($"Updated coefficient c{index + 1} = {val:F3}");
        }
        public void DisplayRangeBasic()
        {

            Console.Write("Enter Basic Variable index (1-based): ");
            int index = int.Parse(Console.ReadLine()) - 1;

            if (index < 0 || index >= numCols - 1 || !basicVars.Contains(index))
            {
                Console.WriteLine("Invalid index or variable is non-basic.");
                return;
            }

            int basicRow = -1;
            for (int i = 1; i < numRows; i++)
            {
                if (Math.Abs(tableau[i, index] - 1.0) < 1e-6 && IsPivotColumn(i, index))
                {
                    basicRow = i;
                    break;
                }
            }

            if (basicRow == -1)
            {
                Console.WriteLine("Error: Basic variable not found in tableau.");
                return;
            }

            // Calculate range for objective coefficient
            double currentCj = -tableau[0, index]; // Original coefficient 
            double deltaLower = double.MaxValue, deltaUpper = double.MaxValue;

            for (int j = 0; j < numCols - 1; j++)
            {
                if (basicVars.Contains(j)) continue; // Skip basic variables
                double aij = tableau[basicRow, j];
                double cjBar = tableau[0, j]; // Reduced cost
                if (aij > 0)
                {
                    deltaUpper = Math.Min(deltaUpper, cjBar / aij);
                }
                else if (aij < 0)
                {
                    deltaLower = Math.Min(deltaLower, -cjBar / aij);
                }
            }

            Console.WriteLine($"Range for c{index + 1}: [{currentCj - deltaLower:F3}, {currentCj + deltaUpper:F3}]");
        }
        public void ChangeBasic()
        {

            Console.Write("Enter Basic Variable index (1-based): ");
            int index = int.Parse(Console.ReadLine()) - 1;
            Console.Write("Enter new coefficient value: ");
            double newVal = double.Parse(Console.ReadLine());

            if (index < 0 || index >= numCols - 1 || !basicVars.Contains(index))
            {
                Console.WriteLine("Invalid index or variable is non-basic.");
                return;
            }

            // Update the objective row coefficient
            tableau[0, index] = -newVal;
            Console.WriteLine($"Updated coefficient c{index + 1} = {newVal:F3}");

        }
        public void DisplayRangeRHS()
        {

            Console.Write("Enter constraint index (1-based): ");
            int index = int.Parse(Console.ReadLine());

            if (index < 1 || index >= numRows)
            {
                Console.WriteLine("Invalid constraint index.");
                return;
            }

            int row = index;
            double currentRHS = tableau[row, numCols - 1];
            double deltaLower = double.MaxValue, deltaUpper = double.MaxValue;

            int slackCol = numCols - numRows + index - 1;
            double shadowPrice = tableau[0, slackCol];

            // Calculate range based on basic variables' feasibility
            for (int j = 0; j < numCols - 1; j++)
            {
                if (!basicVars.Contains(j)) continue;
                int basicRow = GetBasicRow(j);
                if (basicRow == -1) continue;

                double aij = tableau[row, j];
                double bi = tableau[basicRow, numCols - 1];
                if (aij > 0)
                {
                    deltaUpper = Math.Min(deltaUpper, bi / aij);
                }
                else if (aij < 0)
                {
                    deltaLower = Math.Min(deltaLower, -bi / aij);
                }
            }

            Console.WriteLine($"Shadow Price for constraint {index}: {shadowPrice:F3}");
            Console.WriteLine($"Range for RHS {index}: [{currentRHS - deltaLower:F3}, {currentRHS + deltaUpper:F3}]");

        }
        public void ChangeRHS()
        {

            Console.Write("Enter constraint index to change RHS (1-based): ");
            int index = int.Parse(Console.ReadLine());
            Console.Write("Enter new RHS value: ");
            double val = double.Parse(Console.ReadLine());

            if (index < 1 || index >= numRows)
            {
                Console.WriteLine("Invalid constraint index.");
                return;
            }

            tableau[index, numCols - 1] = val;
            Console.WriteLine($"Updated RHS for constraint {index} = {val:F3}");


        }
        public void DisplayRangeNonBasicColumn()
        {

            Console.Write("Enter row (constraint, 1-based): ");
            int row = int.Parse(Console.ReadLine());
            Console.Write("Enter column (non-basic variable, 1-based): ");
            int col = int.Parse(Console.ReadLine()) - 1;

            if (row < 1 || row >= numRows || col < 0 || col >= numCols - 1 || basicVars.Contains(col))
            {
                Console.WriteLine("Invalid row or column, or column is basic.");
                return;
            }

            double currentAij = tableau[row, col];
            double deltaLower = double.MaxValue, deltaUpper = double.MaxValue;

            // Find basic variable in this row
            int basicCol = -1;
            for (int j = 0; j < numCols - 1; j++)
            {
                if (basicVars.Contains(j) && Math.Abs(tableau[row, j] - 1.0) < 1e-6 && IsPivotColumn(row, j))
                {
                    basicCol = j;
                    break;
                }
            }
            if (basicCol == -1)
            {
                Console.WriteLine("Error: No basic variable in row.");
                return;
            }

            double bi = tableau[row, numCols - 1];
            double yij = tableau[0, col]; // Reduced cost
            if (yij > 0)
            {
                deltaUpper = bi / yij;
            }
            else if (yij < 0)
            {
                deltaLower = -bi / yij;
            }

            Console.WriteLine($"Range for a{row},{col + 1}: [{currentAij - deltaLower:F3}, {currentAij + deltaUpper:F3}]");



        }
        public void ChangeNonBasicColumn()
        {

            Console.Write("Enter row (constraint, 1-based): ");
            int row = int.Parse(Console.ReadLine());
            Console.Write("Enter column (non-basic variable, 1-based): ");
            int col = int.Parse(Console.ReadLine()) - 1;
            Console.Write("Enter new value: ");
            double val = double.Parse(Console.ReadLine());

            if (row < 1 || row >= numRows || col < 0 || col >= numCols - 1 || basicVars.Contains(col))
            {
                Console.WriteLine("Invalid row or column, or column is basic.");
                return;
            }

            tableau[row, col] = val;
            Console.WriteLine($"Updated coefficient a{row},{col + 1} = {val:F3}");

        }
        public void AddNewActivity()
        {

            Console.Write("Enter objective function coefficient for new variable: ");
            double objCoef = double.Parse(Console.ReadLine());
            Console.WriteLine("Enter technological coefficients for constraints:");
            double[] techCoefs = new double[numRows - 1];
            for (int i = 1; i < numRows; i++)
            {
                Console.Write($"Coefficient for constraint {i}: ");
                techCoefs[i - 1] = double.Parse(Console.ReadLine());
            }

            // Create new tableau with one more column
            double[,] newTableau = new double[numRows, numCols + 1];
            for (int i = 0; i < numRows; i++)
                for (int j = 0; j < numCols; j++)
                    newTableau[i, j] = tableau[i, j];

            // Add new variable's coefficients
            newTableau[0, numCols - 1] = -objCoef; // Objective row (negated for maximization)
            for (int i = 1; i < numRows; i++)
                newTableau[i, numCols - 1] = techCoefs[i - 1];

            tableau = newTableau;
            numCols++;
            Console.WriteLine($"Added new variable x{numCols - 1}. Must resolve LP to check optimality.");

        }
        public void AddNewConstraint()
        {

            Console.WriteLine("Enter technological coefficients for decision variables:");
            double[] techCoefs = new double[numCols - 1];
            for (int j = 0; j < numCols - 1; j++)
            {
                Console.Write($"Coefficient for x{j + 1}: ");
                techCoefs[j] = double.Parse(Console.ReadLine());
            }
            Console.Write("Enter RHS value: ");
            double rhs = double.Parse(Console.ReadLine());

            // Create new tableau with one more row
            double[,] newTableau = new double[numRows + 1, numCols + 1];
            for (int i = 0; i < numRows; i++)
                for (int j = 0; j < numCols; j++)
                    newTableau[i, j] = tableau[i, j];

            // Add new constraint
            for (int j = 0; j < numCols - 1; j++)
                newTableau[numRows, j] = techCoefs[j];
            newTableau[numRows, numCols - 1] = 1.0; // Slack variable
            newTableau[numRows, numCols] = rhs;

            // Update objective row for new slack variable
            newTableau[0, numCols - 1] = 0.0; // Slack variable has zero objective coefficient

            tableau = newTableau;
            numRows++;
            numCols++;
            basicVars.Add(numCols - 2); // New slack variable is basic
            Console.WriteLine($"Added new constraint {numRows - 1}. Must resolve LP to check feasibility.");


        }
        public void DisplayShadowPrices()
        {

            Console.WriteLine("Shadow Prices:");
            for (int i = 1; i < numRows; i++)
            {
                int slackCol = numCols - numRows + i - 1; // Slack variable column
                double shadowPrice = tableau[0, slackCol];
                Console.WriteLine($"Constraint {i}: {shadowPrice:F3}");
            }

        }
        public void PerformDuality()
        {

            int m = numRows - 1; // Number of constraints
            int n = numCols - numRows; // Number of decision variables

            double[] dualObj = new double[m];
            for (int i = 1; i < numRows; i++)
                dualObj[i - 1] = tableau[i, numCols - 1];

            // Dual constraints (primal coefficients)
            double[,] dualConstraints = new double[n, m];
            double[] dualRHS = new double[n];
            for (int j = 0; j < n; j++)
            {
                for (int i = 1; i < numRows; i++)
                    dualConstraints[j, i - 1] = tableau[i, j];
                dualRHS[j] = -tableau[0, j]; // Primal objective coefficients
            }

            Console.WriteLine("Dual Objective: minimize " + string.Join(" + ", dualObj.Select((b, i) => $"{b:F3}*y{i + 1}")));
            for (int j = 0; j < n; j++)
            {
                Console.WriteLine($"Constraint {j + 1}: " + string.Join(" + ", dualConstraints.GetRow(j).Select((a, i) => $"{a:F3}*y{i + 1}")) + $" >= {dualRHS[j]:F3}");
            }

            // Shadow prices from primal give dual solution
            Console.WriteLine("Dual Solution (from shadow prices):");
            for (int i = 1; i < numRows; i++)
            {
                int slackCol = numCols - numRows + i - 1;
                Console.WriteLine($"y{i} = {tableau[0, slackCol]:F3}");
            }

            // Verify duality
            double primalValue = finalZ;
            double dualValue = 0;
            for (int i = 1; i < numRows; i++)
            {
                int slackCol = numCols - numRows + i - 1;
                dualValue += tableau[i, numCols - 1] * tableau[0, slackCol];
            }
            Console.WriteLine($"Primal Objective Value: {primalValue:F3}");
            Console.WriteLine($"Dual Objective Value: {dualValue:F3}");
            Console.WriteLine(primalValue == dualValue ? "Strong Duality holds." : "Weak Duality holds.");

        }

        private bool IsPivotColumn(int row, int col)
        {
            for (int i = 1; i < numRows; i++)
            {
                if (i != row && Math.Abs(tableau[i, col]) > 1e-6)
                    return false;
            }
            return true;
        }

        private int GetBasicRow(int col)
        {
            for (int i = 1; i < numRows; i++)
            {
                if (Math.Abs(tableau[i, col] - 1.0) < 1e-6 && IsPivotColumn(i, col))
                    return i;
            }
            return -1;
        }



    }

    public static class ArrayExtensions
    {
        public static T[] GetRow<T>(this T[,] array, int row)
        {
            int cols = array.GetLength(1);
            T[] result = new T[cols];
            for (int j = 0; j < cols; j++)
            {
                result[j] = array[row, j];
            }
            return result;
        }
    }
}
