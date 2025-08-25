using System;
using System.Collections.Generic;
using System.Linq;
using LPR_381_Group_V22.Utilities;

public class DualSimplexSolver
{
    private const double EPS = 1e-9;

    /// <summary>
    /// Dual simplex: fix negative RHS. Returns true if all RHS >= 0 at end.
    /// Mutates objectiveRow and constraintRows in-place.
    /// </summary>
    public bool Solve(double[] objectiveRow, List<double[]> constraintRows, int maxIters = 10_000, bool printSteps = true)
    {
        if (objectiveRow == null) throw new ArgumentNullException(nameof(objectiveRow));
        if (constraintRows == null || constraintRows.Count == 0) throw new ArgumentException("No constraint rows.");
        int width = objectiveRow.Length;
        if (constraintRows.Any(r => r.Length != width))
            throw new ArgumentException("All rows (obj & constraints) must have the same length.");

        int iter = 0;

        while (true)
        {
            // 1) Pick pivot row = most negative RHS
            int pivotRow = -1;
            double mostNeg = 0.0;
            for (int r = 0; r < constraintRows.Count; r++)
            {
                double rhs = constraintRows[r][width - 1];
                if (rhs < mostNeg - EPS || (Math.Abs(rhs - mostNeg) <= EPS && pivotRow != -1 && r < pivotRow))
                {
                    mostNeg = rhs;
                    pivotRow = r;
                }
            }

            // All RHS >= 0 → done
            if (pivotRow == -1)
            {
                if (printSteps) Console.WriteLine("Dual phase complete: all RHS >= 0.");
                return true;
            }

            // 2) Pivot column rule:
            //    only negative entries in pivot row,
            //    ratio = Abs(obj[j] / row[j]),
            //    choose smallest NON-ZERO ratio. Tie-break: lowest col index.
            int pivotCol = -1;
            double bestRatio = double.PositiveInfinity;

            for (int j = 0; j < width - 1; j++) // exclude RHS
            {
                double a = constraintRows[pivotRow][j];
                if (a < -EPS)
                {
                    double num = objectiveRow[j];
                    if (Math.Abs(num) > EPS) // ensure non-zero ratio
                    {
                        double ratio = Math.Abs(num / a);
                        if (ratio < bestRatio - EPS ||
                            (Math.Abs(ratio - bestRatio) <= EPS && (pivotCol == -1 || j < pivotCol)))
                        {
                            bestRatio = ratio;
                            pivotCol = j;
                        }
                    }
                }
            }

            if (pivotCol == -1)
            {
                if (printSteps) Console.WriteLine("Infeasible: pivot row has no negative coefficients with non-zero ratios.");
                return false;
            }

            //if (printSteps)
            //{
            //    Console.WriteLine($"\nIteration {++iter}: pivot @ row {pivotRow}, col {pivotCol}");
            //    PrintTableau(objectiveRow, constraintRows);
            //}

            //Pivot(objectiveRow, constraintRows, pivotRow, pivotCol);

            //if (printSteps)
            //{
            //    Console.WriteLine("After pivot:");
            //    PrintTableau(objectiveRow, constraintRows);
            //}
            if (printSteps)
            {
                int n = NumVars(objectiveRow, constraintRows);
                Console.WriteLine($"\nIteration {++iter}: pivot @ constraint {pivotRow + 1}, column {ColLabel(pivotCol, n)}");
                PrintTableau(objectiveRow, constraintRows, n);
            }

            Pivot(objectiveRow, constraintRows, pivotRow, pivotCol);

            if (printSteps)
            {
                int n = NumVars(objectiveRow, constraintRows);
                Console.WriteLine("After pivot:");
                PrintTableau(objectiveRow, constraintRows, NumVars(objectiveRow, constraintRows));
            }


            if (iter >= maxIters)
            {
                if (printSteps) Console.WriteLine("Max iterations reached.");
                return false;
            }
        }
    }


    // ---------- Accessors like the primal solver ----------

    public double[] GetObjectiveRow(double[] objectiveRow, List<double[]> constraintRows,
                                    bool solveIfNeeded = true, int maxIters = 10_000, bool printSteps = false)
    {
        EnsureReady(objectiveRow, constraintRows, solveIfNeeded, maxIters, printSteps);
        return CloneRow(objectiveRow);
    }

    public List<double[]> GetConstraintRows(double[] objectiveRow, List<double[]> constraintRows,
                                            bool solveIfNeeded = true, int maxIters = 10_000, bool printSteps = false)
    {
        EnsureReady(objectiveRow, constraintRows, solveIfNeeded, maxIters, printSteps);
        return CloneRows(constraintRows);
    }

    public (double[] ObjectiveRow, List<double[]> ConstraintRows) GetRows(double[] objectiveRow, List<double[]> constraintRows,
                                                                          bool solveIfNeeded = true, int maxIters = 10_000, bool printSteps = false)
    {
        EnsureReady(objectiveRow, constraintRows, solveIfNeeded, maxIters, printSteps);
        return (CloneRow(objectiveRow), CloneRows(constraintRows));
    }

    private void EnsureReady(double[] objectiveRow, List<double[]> constraintRows, bool solveIfNeeded, int maxIters, bool printSteps)
    {
        if (!solveIfNeeded) return;
        if (AnyNegativeRhs(constraintRows))
        {
            bool ok = Solve(objectiveRow, constraintRows, maxIters, printSteps);
            if (!ok) throw new InvalidOperationException("Dual phase could not reach feasibility (infeasible or max iterations).");
        }
    }

    private static void Pivot(double[] obj, List<double[]> rows, int pr, int pc)
    {
        int width = obj.Length;
        double[] prow = rows[pr];
        double piv = prow[pc];
        if (Math.Abs(piv) <= EPS)
            throw new InvalidOperationException($"Pivot too small/zero at ({pr},{pc}).");

        // Normalize pivot row
        for (int j = 0; j < width; j++) prow[j] /= piv;

        // Eliminate in other rows
        for (int r = 0; r < rows.Count; r++)
        {
            if (r == pr) continue;
            double f = rows[r][pc];
            if (Math.Abs(f) > EPS)
            {
                for (int j = 0; j < width; j++) rows[r][j] -= f * prow[j];
            }
        }

        // Eliminate in objective
        double of = obj[pc];
        if (Math.Abs(of) > EPS)
        {
            for (int j = 0; j < width; j++) obj[j] -= of * prow[j];
        }
    }

    public static bool AnyNegativeRhs(List<double[]> rows)
    {
        if (rows == null || rows.Count == 0) return false;
        int rhs = rows[0].Length - 1;
        return rows.Any(r => r[rhs] < -EPS);
    }

    //public static void PrintTableau(double[] objectiveRow, List<double[]> constraintRows)
    //{
    //    Console.WriteLine("Z: " + string.Join("\t", objectiveRow.Select(v => v.ToString("0.####"))));
    //    for (int i = 0; i < constraintRows.Count; i++)
    //        Console.WriteLine($"x{i + 1}: " + string.Join("\t", constraintRows[i].Select(v => v.ToString("0.####"))));
    //}
    public static void PrintTableau(double[] objectiveRow, List<double[]> constraintRows, int numVars, string title = null)
    {
        int cols = objectiveRow.Length;
        int rows = (constraintRows?.Count ?? 0) + 1;
        var table = new double[rows, cols];

        // Z row
        for (int j = 0; j < cols; j++) table[0, j] = objectiveRow[j];
        // constraints
        for (int i = 0; i < rows - 1; i++)
            for (int j = 0; j < cols; j++)
                table[i + 1, j] = constraintRows[i][j];

        Console.WriteLine(TableIterationFormater.Format(table, numVars, title ?? "Dual Simplex Tableau"));
    }


    private static double[] CloneRow(double[] row)
    {
        var copy = new double[row.Length];
        Array.Copy(row, copy, row.Length);
        return copy;
    }

    private static List<double[]> CloneRows(List<double[]> rows)
    {
        var list = new List<double[]>(rows.Count);
        foreach (var r in rows)
        {
            var c = new double[r.Length];
            Array.Copy(r, c, r.Length);
            list.Add(c);
        }
        return list;
    }
    private static int NumVars(double[] objectiveRow, List<double[]> constraintRows)
    {
        // RHS is the last column; assume one slack per constraint.
        int cols = objectiveRow.Length;              // includes RHS
        int m = constraintRows?.Count ?? 0;          // #constraints (= #slacks)
        return cols - 1 - m;                         // (#non‑RHS) - slacks = decision vars
    }

    private static string ColLabel(int col, int numVars)
    {
        return (col < numVars) ? $"x{col + 1}" : $"t{col - numVars + 1}";
    }

}
