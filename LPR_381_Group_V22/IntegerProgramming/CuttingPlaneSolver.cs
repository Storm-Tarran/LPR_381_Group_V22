using LPR_381_Group_V22.Simplex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LPR_381_Group_V22.IntegerProgramming
{
    public class CuttingPlaneSolver
    {
        private const double IntegerRounder = 1e-9;

        private static double Frac(double a)
        {
            double f = a - Math.Floor(a);
            if (Math.Abs(f) < IntegerRounder || Math.Abs(1 - f) < IntegerRounder) return 0.0;
            return f;
        }

        private static bool IsObjectiveOptimal(double[] obj)
        {
            // all reduced costs (except RHS) >= 0
            for (int j = 0; j < obj.Length - 1; j++)
                if (obj[j] < -IntegerRounder) return false;
            return true;
        }

        private static bool AnyNegativeRhs(List<double[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                double rhs = rows[i][rows[i].Length - 1];
                if (rhs < -IntegerRounder) return true;
            }
            return false;
        }

        private static bool AnyFractionalRhs(List<double[]> rows)
        {
            for (int i = 0; i < rows.Count; i++)
            {
                double rhs = rows[i][rows[i].Length - 1];
                if (Frac(rhs) > IntegerRounder) return true;
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
        public void CuttingPlaneSolution(double[] orow, List<double[]> crows)
        {
            if (orow == null) throw new ArgumentNullException("orow");
            if (crows == null || crows.Count == 0) throw new ArgumentException("No constraint rows.");
            int width = orow.Length;
            for (int i = 0; i < crows.Count; i++)
                if (crows[i].Length != width)
                    throw new ArgumentException("All rows  must have the same length.");

            PrintTableau("Initial tableau:", orow, crows);
            var fractional = new List<Tuple<int, double[], double, double>>();
            for (int i = 0; i < crows.Count; i++)
            {
                double[] row = crows[i];
                double rhs = row[row.Length - 1];
                double rhsFrac = Frac(rhs);
                if (rhsFrac > IntegerRounder)
                    fractional.Add(Tuple.Create(i, row, rhs, rhsFrac));
            }

            if (fractional.Count == 0)
            {
                Console.WriteLine("All RHS are integers.");
                return;
            }
            fractional.Sort((a, b) => Math.Abs(a.Item4 - 0.5).CompareTo(Math.Abs(b.Item4 - 0.5)));
            var chosen = fractional[0];
            int n = chosen.Item2.Length;
            var frac = new double[n];
            for (int j = 0; j < n; j++)
            {
                frac[j] = Frac(chosen.Item2[j]);
            }
            var cut = new double[n];
            for (int j = 0; j < n - 1; j++) cut[j] = -frac[j];
            cut[n - 1] = -frac[n - 1];
            crows.Add(cut);
            int rowCutIndex = crows.Count - 1;
            int pcol = -1;
            double bestRatio = double.PositiveInfinity;

            for (int j = 0; j < n - 1; j++)
            {
                double a = cut[j];
                if (a < -IntegerRounder)
                {
                    double num = orow[j];
                    if (Math.Abs(num) > IntegerRounder) 
                    {
                        double ratio = Math.Abs(num / a);
                        if (ratio < bestRatio - IntegerRounder || (Math.Abs(ratio - bestRatio) <= IntegerRounder && (pcol == -1 || j < pcol)))
                        {
                            bestRatio = ratio;
                            pcol = j;
                        }
                    }
                }
            }

            if (pcol == -1)
            {
                Console.WriteLine("No valid pivot column on the cut .");
                return;
            }
            PrintTableau(string.Format("Before pivot on cut: row {0}, col {1}", rowCutIndex + 1, pcol + 1),orow, crows);

            double piv = crows[rowCutIndex][pcol];
            if (Math.Abs(piv) <= IntegerRounder)
            {
                Console.WriteLine("Pivot too small/zero.");
                return;
            }
            for (int j = 0; j < n; j++)
            {
                crows[rowCutIndex][j] /= piv;
            }
                
            for (int i = 0; i < crows.Count; i++)
            {
                if (i == rowCutIndex) continue;
                double f = crows[i][pcol];
                if (Math.Abs(f) > IntegerRounder)
                {
                    for (int j = 0; j < n; j++)
                        crows[i][j] -= f * crows[rowCutIndex][j];
                }
            }
            {
                double f = orow[pcol];
                if (Math.Abs(f) > IntegerRounder)
                {
                    for (int j = 0; j < n; j++)
                        orow[j] -= f * crows[rowCutIndex][j];
                }
            }
            PrintTableau(string.Format("After pivot on cut: row {0}, col {1}", rowCutIndex + 1, pcol + 1),orow, crows);

            bool needDual = AnyNegativeRhs(crows);
            bool needPrimal = !IsObjectiveOptimal(orow);

            if (needDual)
            {
                Console.WriteLine("Negative RHS running Dual Simplex...");
                var dual = new DualSimplexSolver();
                bool ok = dual.Solve(orow, crows, printSteps: true);
                if (!ok) { Console.WriteLine("Dual Simplex failed."); return; }
                PrintTableau("After Dual Simplex:", orow, crows);
                needPrimal = !IsObjectiveOptimal(orow);
            }

            if (needPrimal)
            {
                Console.WriteLine("Negative is the Objective row Primal Simplex...");
                var primal = new PrimalSimplexSolver2(orow, crows);
                primal.Solve(printSteps: true);

                // copy back updated tableau
                var rowsTuple = primal.GetRows(false); 
                double[] objRow = rowsTuple.orow;
                List<double[]> constrRows = rowsTuple.crows;

                Array.Copy(objRow, orow, orow.Length);
                for (int i = 0; i < crows.Count; i++)
                    Array.Copy(constrRows[i], crows[i], crows[i].Length);

                PrintTableau("After Primal Simplex:", orow, crows);
            }
            if (IsObjectiveOptimal(orow) && !AnyNegativeRhs(crows))
            {
                if (AnyFractionalRhs(crows))
                {
                    Console.WriteLine("Objective optimal but RHS has fractional values proceeding to add additional cut...");
                    CuttingPlaneSolution(orow, crows); 
                    return;
                }

                Console.WriteLine("Displayed the Optimal Tableau.");
                return;
            }

            Console.WriteLine("Cutting-plane step finished");
        }
    }
}
