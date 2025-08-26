using LPR_381_Group_V22.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using IOConstraint = LPR_381_Group_V22.IO.InputFileParser.Constraint;

namespace LPR_381_Group_V22.Simplex
{
    public class PrimalSimplexSolver
    {
        private double[,] tableau;
        private int numVariables, numConstraints;
        private List<int> basicVariables;
        private int iteration = 0;

        public List<string> IterationSnapshots = new List<string>();
        public double FinalZ { get; private set; }
        public List<double> SolutionVector { get; private set; }

        // Adding to allow external use of the final table
        public double[,] FinalTableau { get; private set; }
        public IReadOnlyList<string> FinalLabels => basicVariables.Select(idx => ColLabel(idx)).ToList().AsReadOnly();
        public string FinalTable => (FinalTableau == null) ? "" : TableIterationFormater.Format(FinalTableau, numVariables, "Final Table", FinalLabels);


        public PrimalSimplexSolver(List<double> objective, List<IOConstraint> constraints, bool isMaximization = true)
        {
            numVariables = objective.Count;

            // Convert >= and = constraints by adding artificial variables if needed
            var processedConstraints = new List<IOConstraint>();

            foreach (var constraint in constraints)
            {
                if (constraint.Relation == ">=")
                {
                    // Convert >= to <= by multiplying by -1
                    var newCoeffs = constraint.Coefficients.Select(c => -c).ToList();
                    processedConstraints.Add(new IOConstraint(newCoeffs, "<=", -constraint.RHS));
                }
                else if (constraint.Relation == "=")
                {
                    processedConstraints.Add(new IOConstraint(constraint.Coefficients.ToList(), "<=", constraint.RHS));

                }
                else
                {
                    processedConstraints.Add(new IOConstraint(constraint.Coefficients.ToList(), "<=", constraint.RHS));
                }
            }

            numConstraints = processedConstraints.Count;
            basicVariables = new List<int>();

            int tableauCols = numVariables + numConstraints + 1; // vars + slacks + solution col
            int tableauRows = numConstraints + 1; // constraints + Z-row
            tableau = new double[tableauRows, tableauCols];

            // Fill Objective Row (Z) - negate for maximization
            for (int i = 0; i < numVariables; i++)
                tableau[0, i] = isMaximization ? -objective[i] : objective[i];

            // Fill Constraints
            for (int i = 0; i < numConstraints; i++)
            {
                var constraint = processedConstraints[i];
                for (int j = 0; j < numVariables; j++)
                {
                    if (j < constraint.Coefficients.Count)
                        tableau[i + 1, j] = constraint.Coefficients[j];
                }

                // Slack column e_i
                int slackCol = numVariables + i;
                tableau[i + 1, slackCol] = 1.0;

                basicVariables.Add(slackCol);


                // RHS
                tableau[i + 1, tableauCols - 1] = constraint.RHS;
            }

            // Capture initial tableau
            CaptureSnapshot("Initial Tableau");
        }

        private void CaptureSnapshot(string title)
        {
            IterationSnapshots.Add(TableIterationFormater.Format(tableau, numVariables, title));
        }

        private static double[,] Copy2D(double[,] src)
        {
            int r = src.GetLength(0), c = src.GetLength(1);
            var dst = new double[r, c];
            Array.Copy(src, dst, src.Length);
            return dst;
        }

        public void Solve()
        {
            bool optimal = false;
            iteration = 0;

            while (!optimal)
            {
                int enteringCol = FindEnteringVariable();
                if (enteringCol == -1)
                {
                    optimal = true;
                    FinalZ = tableau[0, tableau.GetLength(1) - 1];
                    SolutionVector = ExtractSolution();

                    FinalTableau = Copy2D(tableau);
                    Console.WriteLine("Optimal Solution Found!");
                    var finalBlock = new StringBuilder();
                    finalBlock.AppendLine(TableIterationFormater.Format(tableau, numVariables, "Final Tableau (Optimal)"));
                    finalBlock.AppendLine(SolutionSummary());

                    IterationSnapshots.Add(finalBlock.ToString());
                    Console.WriteLine(SolutionSummary());
                    Console.WriteLine(new string('-', 100));
                    break;
                }

                int leavingRow = FindLeavingVariable(enteringCol);
                if (leavingRow == -1)
                {
                    Console.WriteLine("Unbounded Solution!");
                    FinalTableau = Copy2D(tableau);
                    IterationSnapshots.Add(TableIterationFormater.Format(tableau, numVariables, "Unbounded Tableau"));
                    break;
                }

                // PRINT: before pivot
                Console.WriteLine($"\nIteration {++iteration}: pivot @ constraint {leavingRow}, column {ColLabel(enteringCol)}");
                Console.WriteLine(TableIterationFormater.Format(tableau, numVariables, "Before pivot"));

                Pivot(leavingRow, enteringCol);
                basicVariables[leavingRow - 1] = enteringCol;

                // PRINT: after pivot
                Console.WriteLine($"After pivot (constraint {leavingRow}, column {ColLabel(enteringCol)}):");
                Console.WriteLine(TableIterationFormater.Format(tableau, numVariables, "After pivot"));

                CaptureSnapshot($"Iteration {iteration} - After pivot");
            }
        }

        private int FindEnteringVariable()
        {
            int enteringCol = -1;
            double mostNegative = 0;
            int totalCols = tableau.GetLength(1) - 1;
            for (int j = 0; j < totalCols; j++)
            {
                if (tableau[0, j] < mostNegative)
                {
                    mostNegative = tableau[0, j];
                    enteringCol = j;
                }
            }
            return enteringCol;

        }

        private int FindLeavingVariable(int enteringCol)
        {
            int leavingRow = -1;
            double minRatio = double.MaxValue;

            int rows = tableau.GetLength(0);
            int rhsCol = tableau.GetLength(1) - 1;

            for (int i = 1; i < rows; i++)
            {
                double a = tableau[i, enteringCol];
                if (a > 1e-9)
                {
                    double ratio = tableau[i, rhsCol] / a;
                    if (ratio >= 0 && ratio < minRatio)
                    {
                        minRatio = ratio;
                        leavingRow = i;
                    }
                }
            }
            return leavingRow;
        }

        private void Pivot(int pivotRow, int pivotCol)
        {
            double pivotElement = tableau[pivotRow, pivotCol];

            // Normalize Pivot Row
            for (int j = 0; j < tableau.GetLength(1); j++)
                tableau[pivotRow, j] /= pivotElement;

            // Eliminate Pivot Column in other rows
            for (int i = 0; i < tableau.GetLength(0); i++)
            {
                if (i != pivotRow)
                {
                    double factor = tableau[i, pivotCol];
                    for (int j = 0; j < tableau.GetLength(1); j++)
                        tableau[i, j] -= factor * tableau[pivotRow, j];
                }
            }
        }

        private List<double> ExtractSolution()
        {
            var solution = new List<double>(new double[numVariables]);

            int rows = tableau.GetLength(0);
            int rhsCol = tableau.GetLength(1) - 1;

            for (int j = 0; j < numVariables; j++)
            {
                // Find if this variable is basic
                int basicRow = -1;
                bool isBasic = true;

                for (int i = 1; i < rows; i++)
                {
                    if (Math.Abs(tableau[i, j] - 1.0) < 1e-9)
                    {
                        if (basicRow == -1)
                            basicRow = i;
                        else
                        {
                            isBasic = false;
                            break;
                        }
                    }
                    else if (Math.Abs(tableau[i, j]) > 1e-9)
                    {
                        isBasic = false;
                        break;
                    }
                }

                if (isBasic && basicRow != -1)
                {
                    solution[j] = tableau[basicRow, rhsCol];
                }
            }

            return solution;
        }
        private string ColLabel(int col)
            => (col < numVariables) ? $"x{col + 1}" : $"t{col - numVariables + 1}";

        private string SolutionSummary(string title = "Optimal solution")
        {
            var sb = new StringBuilder();
            sb.AppendLine(title + ":");
            sb.AppendLine($"Z = {FinalZ:F6}");
            if (SolutionVector != null)
            {
                for (int i = 0; i < numVariables; i++)
                    sb.AppendLine($"x{i + 1} = {SolutionVector[i]:F6}");
            }
            return sb.ToString();
        }

        public double[,] GetFinalTableau()
        {
            // Build a final tableau from the stored tableau[,] 
            return (double[,])tableau.Clone();
        }

        public List<int> BasicVariables
        {
            get { return new List<int>(basicVariables); }
        }
    }
}

