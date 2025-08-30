using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using static LPR_381_Group_V22.IO.InputFileParser;

namespace LPR_381_Group_V22.Simplex
{
    public class RevisedPrimalSimplexSolver
    {
        private const double EPS = 1e-9;


        private readonly double[] cOrig;
        private double[] c;

        // Ax = b data (standard form expects <= turned into slacks; x >= 0)
        private readonly double[,] A;
        private readonly double[] b;

        private readonly int numVariables;
        private readonly int numConstraints;
        private readonly bool isMinimization;

        // Basis bookkeeping 
        private readonly List<int> basicVariables;
        private readonly List<int> nonBasicVariables;

        private double[,] B;
        private double[,] BInverse;
        private double[] cB;
        private double[] xB;

        private double finalZ;
        public List<string> IterationSnapshots { get; private set; } = new List<string>();
        public double FinalZ => finalZ;
        public List<double> SolutionVector { get; private set; } = new List<double>();
        public List<int> BasicVariables => new List<int>(basicVariables);

        public RevisedPrimalSimplexSolver(List<double> objective, List<Constraint> constraints, bool isMinimization)
        {
            if (objective == null || objective.Count == 0) throw new ArgumentException("Objective cannot be null or empty.");
            if (constraints == null || constraints.Count == 0) throw new ArgumentException("Constraints cannot be null or empty.");

            numVariables = objective.Count;
            numConstraints = constraints.Count;
            this.isMinimization = isMinimization;

            cOrig = objective.ToArray();
            c = isMinimization ? cOrig.Select(v => -v).ToArray() : cOrig.ToArray();

            A = new double[numConstraints, numVariables];
            b = new double[numConstraints];
            for (int i = 0; i < numConstraints; i++)
            {
                if (constraints[i].Coefficients.Count != numVariables)
                    throw new ArgumentException($"Constraint {i + 1} has incorrect number of coefficients.");
                for (int j = 0; j < numVariables; j++) A[i, j] = constraints[i].Coefficients[j];
                b[i] = constraints[i].RHS;
            }

            basicVariables = new List<int>(capacity: numConstraints);
            nonBasicVariables = new List<int>(capacity: numVariables + numConstraints);

            B = new double[numConstraints, numConstraints];
            BInverse = new double[numConstraints, numConstraints];
            cB = new double[numConstraints];
            xB = new double[numConstraints];

            // Initial basis = slacks
            for (int i = 0; i < numConstraints; i++)
            {
                B[i, i] = 1.0;
                BInverse[i, i] = 1.0;
                basicVariables.Add(numVariables + i);
                cB[i] = 0.0;
            }
            for (int j = 0; j < numVariables; j++) nonBasicVariables.Add(j);
        }

        public void Solve()
        {
            int iteration = 0;

            while (true)
            {
                // 1) Compute current basic solution and dual prices (pre-pivot quantities)
                xB = MultiplyMatrixVector(BInverse, b);
                if (xB.Any(x => x < -EPS))
                    throw new Exception("Infeasible basis (negative basic value).");

                var y = MultiplyVectorMatrix(cB, BInverse); // y = c_B^T B^{-1}

                // 2) Reduced costs (pre-pivot)
                var rcX_pre = new double[numVariables];
                for (int j = 0; j < numVariables; j++)
                    rcX_pre[j] = c[j] - Dot(y, GetColumn(A, j));

                var rcS_pre = new double[numConstraints];
                for (int k = 0; k < numConstraints; k++)
                    rcS_pre[k] = -y[k]; // slack reduced cost = 0 - y_k

                // 3) Choose entering variable (MAX): strictly positive reduced cost with Bland tie-break
                int enteringIdx = -1;
                double bestPosRC = double.NegativeInfinity;

                foreach (var vIdx in nonBasicVariables.OrderBy(v => v)) // Bland: smallest index first on ties
                {
                    double rc = (vIdx < numVariables) ? rcX_pre[vIdx] : rcS_pre[vIdx - numVariables];
                    if (rc > EPS)
                    {
                        if (enteringIdx == -1 ||
                            rc > bestPosRC + EPS ||
                            (Math.Abs(rc - bestPosRC) <= EPS && vIdx < enteringIdx))
                        {
                            bestPosRC = rc;
                            enteringIdx = vIdx;
                        }
                    }
                }

                // 4) Optimal if no entering variable
                if (enteringIdx == -1)
                {
                    ExtractSolution(); // fills SolutionVector + finalZ (original objective)
                                       // For the "optimal" snapshot: show post quantities = current ones
                    CaptureSnapshot(
                        title: "Optimal",
                        xB: xB,
                        y: y,
                        rcX_post: rcX_pre,
                        rcS_post: rcS_pre,
                        enteringIdx: -1,
                        enteringRC_pre: 0.0,
                        u_pre: new double[numConstraints],
                        ratios_pre: Enumerable.Repeat(double.PositiveInfinity, numConstraints).ToArray(),
                        // ratio labels/leaving label use pre-pivot basis (same as current)
                        basisForRatios_Pre: new List<int>(basicVariables),
                        leavingRow: -1,
                        leavingVarIndex_Pre: -1,
                        zWorking: Dot(cB, xB),
                        zOriginal: finalZ
                    );
                    return;
                }

                // 5) Direction u = B^{-1} a_enter (pre-pivot)
                double[] u_pre = (enteringIdx < numVariables)
                    ? MultiplyMatrixVector(BInverse, GetColumn(A, enteringIdx))
                    : GetColumn(BInverse, enteringIdx - numVariables); // since a_enter = e_k for slack k

                // 6) Ratio test (pre-pivot) with Bland tie-break (smallest basic var index on a tie)
                int leavingRow = -1;
                int leavingVarIndex_Pre = -1;
                double bestRatio = double.MaxValue;
                var ratios_pre = new double[numConstraints];

                for (int i = 0; i < numConstraints; i++)
                {
                    if (u_pre[i] > EPS)
                    {
                        ratios_pre[i] = xB[i] / u_pre[i];
                        if (ratios_pre[i] < bestRatio - EPS ||
                            (Math.Abs(ratios_pre[i] - bestRatio) <= EPS &&
                             (leavingRow == -1 || basicVariables[i] < basicVariables[leavingRow])))
                        {
                            bestRatio = ratios_pre[i];
                            leavingRow = i;
                        }
                    }
                    else
                    {
                        ratios_pre[i] = double.PositiveInfinity;
                    }
                }

                if (leavingRow == -1)
                    throw new Exception("Unbounded problem (no positive component in direction).");

                leavingVarIndex_Pre = basicVariables[leavingRow];
                if (leavingVarIndex_Pre == enteringIdx)
                    throw new Exception("Internal error: entering variable is already basic.");

                // Save the pre-pivot basis for ratio labeling in the snapshot
                var basisForRatios_Pre = new List<int>(basicVariables);

                // Pre-pivot entering reduced cost (for the log line)
                double enteringRC_pre = (enteringIdx < numVariables)
                    ? rcX_pre[enteringIdx]
                    : rcS_pre[enteringIdx - numVariables];

                // 7) Pivot bookkeeping (swap in the basis & nonbasic sets)
                int leavingVar = basicVariables[leavingRow];
                basicVariables[leavingRow] = enteringIdx;
                nonBasicVariables.Remove(enteringIdx);
                if (!nonBasicVariables.Contains(leavingVar))
                    nonBasicVariables.Add(leavingVar);

                // 8) Update B column and cB row (post-pivot B and cB)
                if (enteringIdx < numVariables)
                {
                    var a_col = GetColumn(A, enteringIdx);
                    for (int i = 0; i < numConstraints; i++) B[i, leavingRow] = a_col[i];
                    cB[leavingRow] = c[enteringIdx];
                }
                else
                {
                    int k = enteringIdx - numVariables;
                    for (int i = 0; i < numConstraints; i++) B[i, leavingRow] = (i == k) ? 1.0 : 0.0;
                    cB[leavingRow] = 0.0;
                }

                // 9) Update B^{-1} using the eta-like elementary inverse (post-pivot inverse)
                UpdateBInverse(leavingRow, u_pre);

                // 10) Recompute post-pivot quantities for the printed tableau / Z~ row
                xB = MultiplyMatrixVector(BInverse, b);
                var y_post = MultiplyVectorMatrix(cB, BInverse);

                var rcX_post = new double[numVariables];
                for (int j = 0; j < numVariables; j++)
                    rcX_post[j] = c[j] - Dot(y_post, GetColumn(A, j));

                var rcS_post = new double[numConstraints];
                for (int k = 0; k < numConstraints; k++)
                    rcS_post[k] = -y_post[k];

                // 11) Snapshot AFTER the pivot:
                // - Reduced costs, duals, tableau are post-pivot
                // - The direction/ratios/leaving name are shown from pre-pivot data (correct labeling)
                CaptureSnapshot(
                    title: $"Iteration {iteration + 1}",
                    xB: xB,
                    y: y_post,
                    rcX_post: rcX_post,
                    rcS_post: rcS_post,
                    enteringIdx: enteringIdx,
                    enteringRC_pre: enteringRC_pre,
                    u_pre: u_pre,
                    ratios_pre: ratios_pre,
                    basisForRatios_Pre: basisForRatios_Pre,
                    leavingRow: leavingRow,
                    leavingVarIndex_Pre: leavingVarIndex_Pre,
                    zWorking: Dot(cB, xB),
                    zOriginal: ComputeOriginalZFromCurrentBasis(xB)
                );

                iteration++;
            }
        }

        private double ComputeOriginalZFromCurrentBasis(double[] currentXB)
        {
            var x = new double[numVariables];
            for (int i = 0; i < numConstraints; i++)
            {
                int v = basicVariables[i];
                if (v < numVariables) x[v] = Math.Max(0.0, currentXB[i]);
            }
            return Dot(cOrig, x);
        }

        private void UpdateBInverse(int pivotRow, double[] u)
        {
            double pivot = u[pivotRow];
            if (Math.Abs(pivot) < EPS) throw new Exception("Pivot too small.");

            var E = new double[numConstraints, numConstraints];
            for (int i = 0; i < numConstraints; i++) E[i, i] = 1.0;
            for (int i = 0; i < numConstraints; i++)
                E[i, pivotRow] = (i == pivotRow) ? 1.0 / pivot : -u[i] / pivot;

            BInverse = MultiplyMatrices(E, BInverse);
        }

        private void ExtractSolution()
        {
            var x = new double[numVariables];
            for (int i = 0; i < numConstraints; i++)
            {
                int v = basicVariables[i];
                if (v < numVariables) x[v] = Math.Max(0.0, xB[i]);
            }
            SolutionVector = x.ToList();
            finalZ = Dot(cOrig, x);
        }

        private static string VarLabel(int idx, int n)
        {
            return idx < n ? $"x{idx + 1}" : $"S{idx - n + 1}";
        }

        private void CaptureSnapshot(
    string title,
    double[] xB,
    double[] y,
    double[] rcX_post,
    double[] rcS_post,
    int enteringIdx,
    double enteringRC_pre,
    double[] u_pre,
    double[] ratios_pre,
    List<int> basisForRatios_Pre,
    int leavingRow,
    int leavingVarIndex_Pre,
    double zWorking,
    double zOriginal)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine("Current Tableau (Revised Simplex)");
            sb.AppendLine($"Problem type: {(isMinimization ? "MIN (solving by MAX of -c)" : "MAX")}");
            sb.AppendLine();

            // Dual prices (POST-pivot)
            sb.AppendLine("Dual prices (y = c_B^T B^{-1}):");
            sb.AppendLine(string.Join("\t", y.Select(NumFormat.N3)));
            sb.AppendLine();

            // Reduced costs (POST-pivot)
            sb.AppendLine("Reduced costs:");
            sb.Append("  x: ");
            sb.AppendLine(string.Join("\t", rcX_post.Select(NumFormat.N3)));
            sb.Append("  s: ");
            sb.AppendLine(string.Join("\t", rcS_post.Select(NumFormat.N3)));
            sb.AppendLine();

            if (enteringIdx >= 0)
            {
                string enteringLabel = VarLabel(enteringIdx, numVariables);
                sb.AppendLine($"Entering variable (chosen pre-pivot): {enteringLabel}  (reduced cost pre = {NumFormat.N3(enteringRC_pre)})");
                sb.AppendLine("Direction u = B^{-1} a_enter (pre-pivot):");
                sb.AppendLine(string.Join("\t", u_pre.Select(NumFormat.N3)));
                sb.AppendLine();

                sb.AppendLine("Ratio test (xB_i / u_i; ∞ if u_i ≤ 0)  [labels = pre-pivot basis]:");
                for (int i = 0; i < basisForRatios_Pre.Count; i++)
                {
                    string rowLbl = VarLabel(basisForRatios_Pre[i], numVariables);
                    string rstr = double.IsPositiveInfinity(ratios_pre[i]) ? "∞" : NumFormat.N3(ratios_pre[i]);
                    sb.AppendLine($"{rowLbl}: {rstr}");
                }

                if (leavingRow >= 0 && leavingVarIndex_Pre >= 0)
                {
                    string leavingLabelPre = VarLabel(leavingVarIndex_Pre, numVariables);
                    double pivot = u_pre[leavingRow];
                    sb.AppendLine($"Pivot (pre→post): {leavingLabelPre}  →  {enteringLabel}    (pivot = {NumFormat.N3(pivot)})");
                    sb.AppendLine();
                }
            }

            // Summaries
            sb.AppendLine($"Working objective Z_working (maxified): {NumFormat.N3(zWorking)}");
            sb.AppendLine($"Original objective Z_original ({(isMinimization ? "MIN" : "MAX")}): {NumFormat.N3(zOriginal)}");
            sb.AppendLine();

            // Tableau rows (POST-pivot: B^{-1}A | B^{-1} | RHS)
            var BInvA = MultiplyMatrices(BInverse, A);
            var BInv = (double[,])BInverse.Clone();

            sb.Append("Table\t");
            for (int j = 0; j < numVariables; j++) sb.Append($"x{j + 1}\t");
            for (int j = 0; j < numConstraints; j++) sb.Append($"S{j + 1}\t");
            sb.AppendLine("RHS");

            // Z~ row uses POST-pivot reduced costs and Z
            sb.Append("Z~\t");
            for (int j = 0; j < numVariables; j++) sb.Append($"{NumFormat.N3(rcX_post[j])}\t");
            for (int j = 0; j < numConstraints; j++) sb.Append($"{NumFormat.N3(rcS_post[j])}\t");
            sb.AppendLine(NumFormat.N3(zWorking));

            // Constraint rows: label with the POST-pivot basic variables (current basis)
            for (int i = 0; i < numConstraints; i++)
            {
                int v = basicVariables[i];
                string rowLabel = v < numVariables ? $"x{v + 1}" : $"S{v - numVariables + 1}";
                sb.Append($"{rowLabel}\t");
                for (int j = 0; j < numVariables; j++) sb.Append($"{NumFormat.N3(BInvA[i, j])}\t");
                for (int j = 0; j < numConstraints; j++) sb.Append($"{NumFormat.N3(BInv[i, j])}\t");
                sb.AppendLine(NumFormat.N3(xB[i]));
            }

            sb.AppendLine("Basic Variables: " + string.Join(", ", basicVariables.Select(idx => VarLabel(idx, numVariables))));
            IterationSnapshots.Add(sb.ToString());
        }

        // Linear algebra helpers
        private static double[] GetColumn(double[,] M, int col)
        {
            int rows = M.GetLength(0);
            var v = new double[rows];
            for (int i = 0; i < rows; i++) v[i] = M[i, col];
            return v;
        }

        private static double[] MultiplyMatrixVector(double[,] M, double[] v)
        {
            int rows = M.GetLength(0);
            int cols = M.GetLength(1);
            var r = new double[rows];
            for (int i = 0; i < rows; i++)
            {
                double s = 0;
                for (int j = 0; j < cols; j++) s += M[i, j] * v[j];
                r[i] = s;
            }
            return r;
        }

        private static double[] MultiplyVectorMatrix(double[] v, double[,] M)
        {
            int rows = M.GetLength(0);
            int cols = M.GetLength(1);
            var r = new double[cols];
            for (int j = 0; j < cols; j++)
            {
                double s = 0;
                for (int i = 0; i < rows; i++) s += v[i] * M[i, j];
                r[j] = s;
            }
            return r;
        }

        private static double[,] MultiplyMatrices(double[,] A, double[,] B)
        {
            int rA = A.GetLength(0), cA = A.GetLength(1);
            int rB = B.GetLength(0), cB = B.GetLength(1);
            if (cA != rB) throw new ArgumentException("Dimension mismatch in MultiplyMatrices.");
            var R = new double[rA, cB];
            for (int i = 0; i < rA; i++)
                for (int k = 0; k < cA; k++)
                {
                    double aik = A[i, k];
                    if (Math.Abs(aik) < EPS) continue;
                    for (int j = 0; j < cB; j++)
                        R[i, j] += aik * B[k, j];
                }
            return R;
        }

        private static double Dot(double[] a, double[] b)
        {
            double s = 0;
            for (int i = 0; i < a.Length; i++) s += a[i] * b[i];
            return s;
        }
    }

    public static class NumFormat
    {
        private const double EPS = 1e-12;

        public static string N3(double x)
        {
            if (Math.Abs(x) < EPS) x = 0.0;

            double r = Math.Round(x, 3, MidpointRounding.AwayFromZero);

            if (Math.Abs(r - Math.Round(r)) < EPS)
                return Math.Round(r).ToString(CultureInfo.InvariantCulture);

            return r.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}