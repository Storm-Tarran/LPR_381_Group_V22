using LPR_381_Group_V22.Simplex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR_381_Group_V22.IntegerProgramming
{
    public class CuttingPlaneSolver
    {
        private const double EPS = 1e-9;

        private static double Frac(double a)
        {
            double f = a - Math.Floor(a);
            if (Math.Abs(f) < EPS || Math.Abs(1 - f) < EPS) return 0.0; // treat ~integers as 0 frac
            return f;
        }

        private static bool IsObjectiveOptimal(double[] obj)
        {
            // all reduced costs (except RHS) >= 0
            for (int j = 0; j < obj.Length - 1; j++)
                if (obj[j] < -EPS) return false;
            return true;
        }

        private static bool AnyNegativeRhs(List<double[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                double rhs = rows[i][rows[i].Length - 1];
                if (rhs < -EPS) return true;
            }
            return false;
        }

        private static bool AnyFractionalRhs(List<double[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                double rhs = rows[i][rows[i].Length - 1];
                if (Frac(rhs) > EPS) return true;
            }
            return false;
        }

        private static void PrintTableau(string title, double[] obj, List<double[]> rows)
        {
            Console.WriteLine(title);
            Console.WriteLine("z:  " + string.Join("\t", obj.Select(v => v.ToString("0.####"))));
            for (int i = 0; i < rows.Count; i++)
                Console.WriteLine($"r{i + 1}: " + string.Join("\t", rows[i].Select(v => v.ToString("0.####"))));
            Console.WriteLine();
        }

        /// <summary>
        /// One Gomory fractional cut iteration:
        /// - Choose a constraint with fractional RHS (closest to 0.5),
        /// - Add cut = -frac(row),
        /// - Pivot on that cut using smallest NON-ZERO |obj[j]/cut[j]| with cut[j] < 0,
        /// - If needed, run Dual Simplex (fix negative RHS) then Primal Simplex (optimize).
        /// Mutates objectiveRow and constraintRows in-place.
        /// </summary>
        public void CuttingPlaneSolution(double[] objectiveRow, List<double[]> constraintRows)
        {
            // Sanity
            if (objectiveRow == null) throw new ArgumentNullException("objectiveRow");
            if (constraintRows == null || constraintRows.Count == 0) throw new ArgumentException("No constraint rows.");
            int width = objectiveRow.Length;
            for (int i = 0; i < constraintRows.Count; i++)
                if (constraintRows[i].Length != width)
                    throw new ArgumentException("All rows (objective & constraints) must have the same length.");

            PrintTableau("Initial tableau:", objectiveRow, constraintRows);

            // 1) rows with fractional RHS
            var fractional = new List<Tuple<int, double[], double, double>>(); // (idx, row, rhs, rhsFrac)
            for (int i = 0; i < constraintRows.Count; i++)
            {
                double[] row = constraintRows[i];
                double rhs = row[row.Length - 1];
                double rhsFrac = Frac(rhs);
                if (rhsFrac > EPS)
                    fractional.Add(Tuple.Create(i, row, rhs, rhsFrac));
            }

            if (fractional.Count == 0)
            {
                Console.WriteLine("All RHS are integers. No Gomory cut needed.");
                return;
            }

            // 2) pick RHS frac closest to 0.5
            fractional.Sort((a, b) =>
                Math.Abs(a.Item4 - 0.5).CompareTo(Math.Abs(b.Item4 - 0.5)));
            var chosen = fractional[0];
            int n = chosen.Item2.Length;

            // 3) fractional parts of chosen row
            var frac = new double[n];
            for (int j = 0; j < n; j++)
                frac[j] = Frac(chosen.Item2[j]);

            // 4) build cut = -frac(row), including RHS
            var cut = new double[n];
            for (int j = 0; j < n - 1; j++) cut[j] = -frac[j];
            cut[n - 1] = -frac[n - 1];

            // 5) append cut
            constraintRows.Add(cut);
            int cutRowIdx = constraintRows.Count - 1;

            // 6) choose pivot column on the cut row (negative entries only), smallest NON-ZERO |obj[j]/cut[j]|
            int pivotCol = -1;
            double bestRatio = double.PositiveInfinity;
            for (int j = 0; j < n - 1; j++)
            {
                double a = cut[j];
                if (a < -EPS)
                {
                    double num = objectiveRow[j];
                    if (Math.Abs(num) > EPS) // ensure ratio is not zero
                    {
                        double ratio = Math.Abs(num / a);
                        if (ratio < bestRatio - EPS || (Math.Abs(ratio - bestRatio) <= EPS && (pivotCol == -1 || j < pivotCol)))
                        {
                            bestRatio = ratio;
                            pivotCol = j;
                        }
                    }
                }
            }

            if (pivotCol == -1)
            {
                Console.WriteLine("No valid pivot column on the cut (need a negative cut coeff with non-zero obj coeff).");
                return;
            }

            // 7) pivot on (cutRowIdx, pivotCol)
            PrintTableau(
                string.Format("Before pivot on cut: row {0}, col {1}", cutRowIdx + 1, pivotCol + 1),
                objectiveRow, constraintRows);

            double piv = constraintRows[cutRowIdx][pivotCol];
            if (Math.Abs(piv) <= EPS)
            {
                Console.WriteLine("Pivot too small/zero.");
                return;
            }

            // normalize cut row
            for (int j = 0; j < n; j++)
                constraintRows[cutRowIdx][j] /= piv;

            // eliminate pivot column from other constraints
            for (int i = 0; i < constraintRows.Count; i++)
            {
                if (i == cutRowIdx) continue;
                double f = constraintRows[i][pivotCol];
                if (Math.Abs(f) > EPS)
                {
                    for (int j = 0; j < n; j++)
                        constraintRows[i][j] -= f * constraintRows[cutRowIdx][j];
                }
            }

            // eliminate pivot column from objective
            {
                double f = objectiveRow[pivotCol];
                if (Math.Abs(f) > EPS)
                {
                    for (int j = 0; j < n; j++)
                        objectiveRow[j] -= f * constraintRows[cutRowIdx][j];
                }
            }

            PrintTableau(
                string.Format("After pivot on cut: row {0}, col {1}", cutRowIdx + 1, pivotCol + 1),
                objectiveRow, constraintRows);

            // 8) cleanup: Dual then Primal if needed
            bool needDual = AnyNegativeRhs(constraintRows);
            bool needPrimal = !IsObjectiveOptimal(objectiveRow);

            if (needDual)
            {
                Console.WriteLine("Negative RHS detected → running Dual Simplex...");
                var dual = new DualSimplexSolver();
                bool ok = dual.Solve(objectiveRow, constraintRows, printSteps: true);
                if (!ok) { Console.WriteLine("Dual Simplex failed (infeasible or max iters)."); return; }
                PrintTableau("After Dual Simplex:", objectiveRow, constraintRows);
                needPrimal = !IsObjectiveOptimal(objectiveRow);
            }

            if (needPrimal)
            {
                Console.WriteLine("Negative reduced cost detected → running Primal Simplex...");
                var primal = new PrimalSimplexSolver2(objectiveRow, constraintRows);
                primal.Solve(printSteps: true);

                // copy back updated tableau
                var rowsTuple = primal.GetRows(false); // returns (double[] ObjectiveRow, List<double[]> ConstraintRows)
                double[] objRow = rowsTuple.ObjectiveRow;
                List<double[]> constrRows = rowsTuple.ConstraintRows;

                Array.Copy(objRow, objectiveRow, objectiveRow.Length);
                for (int i = 0; i < constraintRows.Count; i++)
                    Array.Copy(constrRows[i], constraintRows[i], constraintRows[i].Length);

                PrintTableau("After Primal Simplex:", objectiveRow, constraintRows);
            }

            // 9) If now optimal & feasible but RHS still fractional, add another cut
            if (IsObjectiveOptimal(objectiveRow) && !AnyNegativeRhs(constraintRows))
            {
                if (AnyFractionalRhs(constraintRows))
                {
                    Console.WriteLine("Objective optimal but RHS has fractional values → adding another Gomory cut...");
                    CuttingPlaneSolution(objectiveRow, constraintRows); // recursive step
                    return;
                }

                Console.WriteLine("Displayed the Optimal Tableau.");
                return;
            }

            Console.WriteLine("Cutting-plane step finished (further steps may be required).");
        }
    }
}
