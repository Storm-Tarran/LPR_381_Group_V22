using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LPR_381_Group_V22.IO.InputFileParser;

namespace LPR_381_Group_V22.Simplex
{
    public class PrimalSimplexSolver
    {
        private double[,] tableau;
        private int numVariables, numConstraints;
        private List<string> basicVariables;

        public List<string> IterationSnapshots = new List<string>();
        public double FinalZ { get; private set; }
        public List<double> SolutionVector { get; private set; }

        public PrimalSimplexSolver(List<double> objective, List<Constraint> constraints)
        {
            numVariables = objective.Count;
            numConstraints = constraints.Count;
            basicVariables = new List<string>();

            int tableauCols = numVariables + numConstraints + 1; // vars + slacks + solution col
            int tableauRows = numConstraints + 1; // constraints + Z-row
            tableau = new double[tableauRows, tableauCols];

            // Fill Objective Row (Z)
            for (int i = 0; i < numVariables; i++)
                tableau[0, i] = -objective[i]; // Negated for maximization

            // Fill Constraints
            for (int i = 0; i < numConstraints; i++)
            {
                var constraint = constraints[i];
                for (int j = 0; j < numVariables; j++)
                    tableau[i + 1, j] = constraint.Coefficients[j];

                // Slack Variable (identity matrix)
                tableau[i + 1, numVariables + i] = 1;
                basicVariables.Add($"S{i + 1}");

                // RHS
                tableau[i + 1, tableauCols - 1] = constraint.RHS;
            }
        }

        public void Solve()
        {
            bool optimal = false;
            while (!optimal)
            {
                CaptureSnapshot();
                int enteringCol = FindEnteringVariable();
                if (enteringCol == -1)
                {
                    optimal = true;
                    FinalZ = tableau[0, tableau.GetLength(1) - 1];
                    SolutionVector = ExtractSolution();
                    break;
                }

                int leavingRow = FindLeavingVariable(enteringCol);
                if (leavingRow == -1)
                {
                    Console.WriteLine("Unbounded Solution!");
                    break;
                }

                Pivot(leavingRow, enteringCol);
                basicVariables[leavingRow - 1] = $"x{enteringCol + 1}";
            }
        }

        private int FindEnteringVariable()
        {
            int enteringCol = -1;
            double mostNegative = 0;

            for (int j = 0; j < numVariables + numConstraints; j++)
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

            for (int i = 1; i <= numConstraints; i++)
            {
                double colValue = tableau[i, enteringCol];
                if (colValue > 0)
                {
                    double ratio = tableau[i, tableau.GetLength(1) - 1] / colValue;
                    if (ratio < minRatio)
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
            for (int i = 0; i < basicVariables.Count; i++)
            {
                if (basicVariables[i].StartsWith("x"))
                {
                    int varIndex = int.Parse(basicVariables[i].Substring(1)) - 1;
                    solution[varIndex] = tableau[i + 1, tableau.GetLength(1) - 1];
                }
            }
            return solution;
        }

        private void CaptureSnapshot()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Current Tableau:");

            sb.Append("Table\t");
            for (int j = 0; j < numVariables; j++) sb.Append($"x{j + 1}\t");
            for (int j = 0; j < numConstraints; j++) sb.Append($"S{j + 1}\t");
            sb.AppendLine("RHS");

            sb.Append("Z\t");
            for (int j = 0; j < tableau.GetLength(1); j++)
                sb.Append($"{tableau[0, j]:0.###}\t");
            sb.AppendLine();

            for (int i = 1; i < tableau.GetLength(0); i++)
            {
                sb.Append($"{i}\t");
                for (int j = 0; j < tableau.GetLength(1); j++)
                    sb.Append($"{tableau[i, j]:0.###}\t");
                sb.AppendLine();
            }

            IterationSnapshots.Add(sb.ToString());
        }

    }
}
