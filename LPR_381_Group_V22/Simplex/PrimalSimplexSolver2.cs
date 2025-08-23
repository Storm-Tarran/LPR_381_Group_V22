using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LPR_381_Group_V22.Simplex
{
    public class PrimalSimplexSolver2
    {
        private const double EPS = 1e-10;

        private double[,] tableau;      // includes RHS as last column
        private int rows, cols;         // rows = constraints + 1 (Z), cols = vars + RHS
        private bool _isOptimal = false;

        public List<string> IterationSnapshots { get; } = new List<string>();
        public double FinalZ { get; private set; }
        public List<double> SolutionVector { get; private set; } = new List<double>();

        /// <summary>
        /// Build directly from a complete tableau:
        /// orow length == each constraint row length; last entry is RHS.
        /// </summary>
        public PrimalSimplexSolver2(double[] orow, List<double[]> crows)
        {
            if (orow == null) throw new ArgumentNullException(nameof(orow));
            if (crows == null || crows.Count == 0) throw new ArgumentException("No constraint rows.");
            int w = orow.Length;
            if (crows.Any(r => r.Length != w))
                throw new ArgumentException("All rows (obj & constraints) must have the same length.");

            rows = crows.Count + 1;
            cols = w;
            tableau = new double[rows, cols];

            // Copy objective (assume already negated for max if needed)
            for (int j = 0; j < cols; j++) tableau[0, j] = orow[j];

            // Copy constraints
            for (int i = 0; i < crows.Count; i++)
                for (int j = 0; j < cols; j++)
                    tableau[i + 1, j] = crows[i][j];
        }

        /// <summary>Classic primal simplex. Returns true if optimal reached.</summary>
        public bool Solve(int maxIters = 10_000, bool printSteps = false)
        {
            int iter = 0;
            while (true)
            {
                CaptureSnapshot($"Start of iter {iter}");

                int pivotCol = FindEnteringColumn();
                if (pivotCol == -1)
                {
                    FinalZ = tableau[0, cols - 1];
                    _isOptimal = true;
                    if (printSteps) Console.WriteLine("Optimal reached.");
                    return true;
                }

                int pivotRow = FindLeavingRow(pivotCol);
                if (pivotRow == -1)
                {
                    if (printSteps) Console.WriteLine("Unbounded: no valid leaving row.");
                    _isOptimal = false;
                    return false;
                }

                // BEFORE PIVOT snapshot
                CaptureSnapshot($"Before pivot (iter {iter + 1}) at row {pivotRow}, col {pivotCol}");

                if (printSteps)
                {
                    Console.WriteLine($"\nIter {++iter}: pivot @ row {pivotRow}, col {pivotCol}");
                    PrintTableau();
                }

                Pivot(pivotRow, pivotCol);

                // AFTER PIVOT snapshot
                CaptureSnapshot($"After pivot (iter {iter}) at row {pivotRow}, col {pivotCol}");

                if (printSteps)
                {
                    Console.WriteLine("After pivot:");
                    PrintTableau();
                }

                if (iter >= maxIters)
                {
                    if (printSteps) Console.WriteLine("Max iterations reached.");
                    _isOptimal = false;
                    return false;
                }
            }
        }

        // ---------- Selection rules ----------

        // Most negative in objective row (exclude RHS). Tie-break: lowest column index.
        private int FindEnteringColumn()
        {
            int rhs = cols - 1;
            int pivotCol = -1;
            double mostNeg = 0.0;
            for (int j = 0; j < rhs; j++)
            {
                double c = tableau[0, j];
                if (c < mostNeg - EPS || (Math.Abs(c - mostNeg) <= EPS && pivotCol != -1 && j < pivotCol))
                {
                    mostNeg = c;
                    pivotCol = j;
                }
            }
            return pivotCol; // -1 => optimal
        }

        // Smallest non-zero ratio RHS / a_ij where a_ij > 0. Tie-break: lowest row index.
        private int FindLeavingRow(int pivotCol)
        {
            int rhs = cols - 1;
            int bestRow = -1;
            double bestRatio = double.PositiveInfinity;

            for (int i = 1; i < rows; i++)
            {
                double a = tableau[i, pivotCol];
                if (a > EPS)
                {
                    double ratio = tableau[i, rhs] / a;
                    if ((ratio > EPS && ratio < bestRatio - EPS) ||
                        (Math.Abs(ratio - bestRatio) <= EPS && bestRow == -1 ? true : i < bestRow))
                    {
                        bestRatio = ratio;
                        bestRow = i;
                    }
                }
            }
            return bestRow; // -1 => unbounded
        }

        // ---------- Pivot operation ----------

        private void Pivot(int pr, int pc)
        {
            double piv = tableau[pr, pc];
            if (Math.Abs(piv) <= EPS)
                throw new InvalidOperationException($"Pivot too small/zero at ({pr},{pc}).");

            // Normalize pivot row
            for (int j = 0; j < cols; j++)
                tableau[pr, j] /= piv;

            // Eliminate
            for (int i = 0; i < rows; i++)
            {
                if (i == pr) continue;
                double f = tableau[i, pc];
                if (Math.Abs(f) <= EPS) continue;
                for (int j = 0; j < cols; j++)
                    tableau[i, j] -= f * tableau[pr, j];
            }
        }

        // ---------- Snapshots / Debug ----------

        private void CaptureSnapshot(string title)
        {
            var sb = new StringBuilder();
            int c = cols, r = rows;

            if (!string.IsNullOrWhiteSpace(title))
                sb.AppendLine(title);

            sb.AppendLine("Current Tableau:");
            sb.AppendLine("OBJ\t" + string.Join("\t", Enumerable.Range(0, c).Select(j => tableau[0, j].ToString("0.###"))));
            for (int i = 1; i < r; i++)
                sb.AppendLine($"r{i}\t" + string.Join("\t", Enumerable.Range(0, c).Select(j => tableau[i, j].ToString("0.###"))));

            IterationSnapshots.Add(sb.ToString());
        }

        private void PrintTableau()
        {
            Console.WriteLine("OBJ: " + string.Join("\t", Enumerable.Range(0, cols).Select(j => tableau[0, j].ToString("0.####"))));
            for (int i = 1; i < rows; i++)
                Console.WriteLine($"r{i}: " + string.Join("\t", Enumerable.Range(0, cols).Select(j => tableau[i, j].ToString("0.####"))));
        }

        // ---------- Getters ----------

        public double[] Getorow(bool solveIfNeeded = true)
        {
            EnsureReady(solveIfNeeded);
            var row = new double[cols];
            for (int j = 0; j < cols; j++) row[j] = tableau[0, j];
            return row;
        }

        public List<double[]> Getcrows(bool solveIfNeeded = true)
        {
            EnsureReady(solveIfNeeded);
            var list = new List<double[]>(rows - 1);
            for (int i = 1; i < rows; i++)
            {
                var r = new double[cols];
                for (int j = 0; j < cols; j++) r[j] = tableau[i, j];
                list.Add(r);
            }
            return list;
        }

        public (double[] orow, List<double[]> crows) GetRows(bool solveIfNeeded = true)
        {
            EnsureReady(solveIfNeeded);
            return (Getorow(false), Getcrows(false));
        }

        private void EnsureReady(bool solveIfNeeded)
        {
            if (!_isOptimal && solveIfNeeded)
            {
                if (!Solve())
                    throw new InvalidOperationException("Could not reach an optimal tableau (unbounded or infeasible).");
            }
        }
    }
}
