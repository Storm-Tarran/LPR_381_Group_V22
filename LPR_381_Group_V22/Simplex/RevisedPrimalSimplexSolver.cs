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

        // Original objective (for final Z sign) and working objective (always maximize)
        private readonly double[] cOrig;
        private double[] c;

        // Ax = b data (standard form expects <= turned into slacks; x >= 0)
        private readonly double[,] A; // m x n
        private readonly double[] b;   // m

        private readonly int numVariables;   // n
        private readonly int numConstraints; // m
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
                // 1) Compute basic solution and dual prices
                xB = MultiplyMatrixVector(BInverse, b);
                if (xB.Any(x => x < -EPS)) throw new Exception("Infeasible basis (negative basic value).");

                var y = MultiplyVectorMatrix(cB, BInverse); // cB^T B^{-1}

                // 2) Reduced costs for all variables (x and slacks)
                var rcX = new double[numVariables];
                for (int j = 0; j < numVariables; j++)
                    rcX[j] = c[j] - Dot(y, GetColumn(A, j));

                var rcS = new double[numConstraints];
                for (int k = 0; k < numConstraints; k++)
                    rcS[k] = -y[k]; // reduced costs of slack columns

                // 3) Choose entering variable (for working MAX): pick most positive reduced cost
                int enteringIdx = -1;
                double bestPosRC = EPS; // require strictly > EPS to enter

                foreach (var vIdx in nonBasicVariables.OrderBy(v => v)) // Bland tie-break
                {
                    double rc = (vIdx < numVariables) ? rcX[vIdx] : rcS[vIdx - numVariables];
                    if (rc > bestPosRC + EPS ||
                        (Math.Abs(rc - bestPosRC) <= EPS && enteringIdx != -1 && vIdx < enteringIdx))
                    {
                        bestPosRC = rc;
                        enteringIdx = vIdx;
                    }
                }

                // If no entering variable, optimal for the working (max) problem
                if (enteringIdx == -1)
                {
                    ExtractSolution(); // fills SolutionVector and finalZ using original c
                    // *** Final snapshot of basis & solution (clear, at optimal) ***
                    CaptureSnapshot(
                        title: $"Iteration {iteration} (optimal)",
                        xB: xB,
                        y: y,
                        rcX: rcX,
                        rcS: rcS,
                        enteringIdx: -1,
                        u: new double[numConstraints],
                        ratios: Enumerable.Repeat(double.PositiveInfinity, numConstraints).ToArray(),
                        leavingRow: -1,
                        zWorking: Dot(cB, xB),
                        zOriginal: finalZ
                    );
                    return;
                }

                // 4) Direction u = B^{-1} a_enter (or B^{-1} e_k for slack enter)
                double[] u = (enteringIdx < numVariables)
                    ? MultiplyMatrixVector(BInverse, GetColumn(A, enteringIdx))
                    : GetColumn(BInverse, enteringIdx - numVariables);

                // 5) Ratio test with Bland: pick leaving row
                int leavingRow = -1;
                double bestRatio = double.MaxValue;
                var ratios = new double[numConstraints];

                for (int i = 0; i < numConstraints; i++)
                {
                    if (u[i] > EPS)
                    {
                        ratios[i] = xB[i] / u[i];
                        if (ratios[i] < bestRatio - EPS ||
                            (Math.Abs(ratios[i] - bestRatio) <= EPS &&
                             (leavingRow == -1 || basicVariables[i] < basicVariables[leavingRow])))
                        {
                            bestRatio = ratios[i];
                            leavingRow = i;
                        }
                    }
                    else
                    {
                        ratios[i] = double.PositiveInfinity;
                    }
                }

                if (leavingRow == -1) throw new Exception("Unbounded (no positive component in direction).");

                // PRE-PIVOT snapshot (shows which will enter/leave) ***
                CaptureSnapshot(
                    title: $"Iteration {iteration} (pre-pivot)",
                    xB: xB,
                    y: y,
                    rcX: rcX,
                    rcS: rcS,
                    enteringIdx: enteringIdx,
                    u: u,
                    ratios: ratios,
                    leavingRow: leavingRow,
                    zWorking: Dot(cB, xB),
                    zOriginal: ComputeOriginalZFromCurrentBasis(xB)
                );

                // 6) Pivot bookkeeping (basis swap & sets)  // ***
                int leavingVar = basicVariables[leavingRow];
                basicVariables[leavingRow] = enteringIdx;
                nonBasicVariables.Remove(enteringIdx);
                if (!nonBasicVariables.Contains(leavingVar))
                    nonBasicVariables.Add(leavingVar);

                // 7) Update B column and cB row
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

                // 8) Update B^{-1} via Eta (using u)
                UpdateBInverse(leavingRow, u);

                // *** POST-PIVOT recompute & snapshot so labels show the new basis ***
                xB = MultiplyMatrixVector(BInverse, b);
                var yPost = MultiplyVectorMatrix(cB, BInverse);
                var rcXPost = new double[numVariables];
                for (int j = 0; j < numVariables; j++)
                    rcXPost[j] = c[j] - Dot(yPost, GetColumn(A, j));
                var rcSPost = new double[numConstraints];
                for (int k = 0; k < numConstraints; k++)
                    rcSPost[k] = -yPost[k];

                CaptureSnapshot(
                    title: $"Iteration {iteration} (post-pivot)",
                    xB: xB,
                    y: yPost,
                    rcX: rcXPost,
                    rcS: rcSPost,
                    enteringIdx: enteringIdx,
                    u: u,
                    ratios: ratios,
                    leavingRow: leavingRow,
                    zWorking: Dot(cB, xB),
                    zOriginal: ComputeOriginalZFromCurrentBasis(xB)
                );

                iteration++;
            }
        }

        private double ComputeOriginalZFromCurrentBasis(double[] currentXB)
        {
            // Build full x (nonbasics = 0)
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

        // Snapshot (now takes a title so we can tag pre/post/optimal)  // ***
        private void CaptureSnapshot(
            string title,
            double[] xB,
            double[] y,
            double[] rcX,
            double[] rcS,
            int enteringIdx,
            double[] u,
            double[] ratios,
            int leavingRow,
            double zWorking,
            double zOriginal)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title);
            sb.AppendLine("Current Tableau (Revised Simplex)");
            sb.AppendLine($"Problem type: {(isMinimization ? "MIN (solving by MAX of -c)" : "MAX")}");
            sb.AppendLine();

            // Dual prices
            sb.AppendLine("Dual prices (y = c_B^T B^{-1}):");
            sb.AppendLine(string.Join("\t", y.Select(NumFormat.N3)));
            sb.AppendLine();

            // Reduced costs for x and slacks
            sb.AppendLine("Reduced costs:");
            sb.Append("  x: ");
            sb.AppendLine(string.Join("\t", rcX.Select(NumFormat.N3)));
            sb.Append("  s: ");
            sb.AppendLine(string.Join("\t", rcS.Select(NumFormat.N3)));
            sb.AppendLine();

            if (enteringIdx >= 0)
            {
                string enteringLabel = VarLabel(enteringIdx, numVariables);
                double enteringRC = (enteringIdx < numVariables) ? rcX[enteringIdx] : rcS[enteringIdx - numVariables];
                sb.AppendLine($"Entering variable: {enteringLabel}  (reduced cost = {NumFormat.N3(enteringRC)})");
                sb.AppendLine("Direction u = B^{-1} a_enter:");
                sb.AppendLine(string.Join("\t", u.Select(NumFormat.N3)));
                sb.AppendLine();

                sb.AppendLine("Ratio test (xB_i / u_i; ∞ if u_i ≤ 0):");
                for (int i = 0; i < numConstraints; i++)
                {
                    string rowLbl = VarLabel(basicVariables[i], numVariables);
                    string rstr = double.IsPositiveInfinity(ratios[i]) ? "∞" : NumFormat.N3(ratios[i]);
                    sb.AppendLine($"{rowLbl}: {rstr}");
                }

                if (leavingRow >= 0)
                {
                    string leavingLabel = VarLabel(basicVariables[leavingRow], numVariables);
                    double pivot = u[leavingRow];
                    sb.AppendLine($"Leaving variable: {leavingLabel}  (pivot = {NumFormat.N3(pivot)})");
                    sb.AppendLine();
                }
            }

            // Summaries
            sb.AppendLine($"Working objective Z_working (maxified): {NumFormat.N3(zWorking)}");
            sb.AppendLine($"Original objective Z_original ({(isMinimization ? "MIN" : "MAX")}): {NumFormat.N3(zOriginal)}");
            sb.AppendLine();

            // A compact revised-tableau view: B^{-1}A | B^{-1} | RHS
            var BInvA = MultiplyMatrices(BInverse, A);
            var BInv = (double[,])BInverse.Clone();

            sb.Append("Table\t");
            for (int j = 0; j < numVariables; j++) sb.Append($"x{j + 1}\t");
            for (int j = 0; j < numConstraints; j++) sb.Append($"S{j + 1}\t");
            sb.AppendLine("RHS");

            // Z row (reduced costs and working z)
            sb.Append("Z~\t");
            for (int j = 0; j < numVariables; j++) sb.Append($"{NumFormat.N3(rcX[j])}\t");
            for (int j = 0; j < numConstraints; j++) sb.Append($"{NumFormat.N3(rcS[j])}\t");
            sb.AppendLine(NumFormat.N3(zWorking));

            // Constraint rows (label from CURRENT basis)
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

        // ---------- Linear algebra helpers ----------
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

       
        // Round to 3 decimals; print integers without decimals; trim trailing zeros.
        
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
