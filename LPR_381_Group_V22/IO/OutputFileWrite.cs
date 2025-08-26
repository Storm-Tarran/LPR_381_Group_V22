using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using LPR_381_Group_V22.Simplex;
using LPR_381_Group_V22.Utilities;
using static LPR_381_Group_V22.IO.InputFileParser;

namespace LPR_381_Group_V22.IO
{
    public static class OutputFileWrite
    {
        
        public static void WriteFullResults(
            string filePath,
            string solverUsed,
            string problemType,
            List<double> objectiveCoefficients,
            List<Constraint> constraints,
            List<string> signRestrictions,
            List<string> iterationSnapshots,
            double finalZ,
            List<double> solutionVector,
            bool append = false)
        {
            EnsureDirectory(filePath);

            var sb = new StringBuilder();

            // Header
            sb.AppendLine("============================================================");
            sb.AppendLine($"Solver: {solverUsed}");
            sb.AppendLine($"Problem type: {problemType}");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("============================================================");

            // Canonical form (as text)
            try
            {
                sb.Append(CanonicalFormConverter.CanonicalFormForFile(
                    problemType,
                    objectiveCoefficients,
                    constraints,
                    signRestrictions));
            }
            catch
            {
                // If for any reason canonical form fails, don't block output
                sb.AppendLine("[Canonical form unavailable]");
            }

            // Iterations
            if (iterationSnapshots != null && iterationSnapshots.Count > 0)
            {
                sb.AppendLine("=== Iteration Snapshots ===");
                for (int i = 0; i < iterationSnapshots.Count; i++)
                {
                    sb.AppendLine($"--- Iteration {i + 1} ---");
                    sb.AppendLine(iterationSnapshots[i]);
                }
                sb.AppendLine();
            }

            // Final results
            sb.AppendLine("=== Final Results ===");
            sb.AppendLine($"Z* = {NumFormat.N3(finalZ)}");

            if (solutionVector != null && solutionVector.Count > 0)
            {
                for (int i = 0; i < solutionVector.Count; i++)
                    sb.AppendLine($"x{i + 1} = {NumFormat.N3(solutionVector[i])}");
            }

            // Write
            WriteToFile(filePath, sb.ToString(), append);
        }

        /// <summary>
        /// Minimal writer when you just have a block of text (snapshots), Z, and x.
        /// </summary>
        public static void WriteSnapshotsOnly(
            string filePath,
            string solverUsed,
            List<string> snapshots,
            double finalZ,
            List<double> solutionVector,
            bool append = true)
        {
            EnsureDirectory(filePath);

            var sb = new StringBuilder();
            sb.AppendLine("============================================================");
            sb.AppendLine($"Solver: {solverUsed}");
            sb.AppendLine($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine("============================================================");

            if (snapshots != null && snapshots.Count > 0)
            {
                sb.AppendLine("=== Solver Log ===");
                foreach (var s in snapshots)
                {
                    sb.AppendLine(s);
                    if (!s.EndsWith("\n")) sb.AppendLine();
                }
            }

            sb.AppendLine("=== Final Results ===");
            sb.AppendLine($"Z* = {NumFormat.N3(finalZ)}");

            if (solutionVector != null && solutionVector.Count > 0)
            {
                for (int i = 0; i < solutionVector.Count; i++)
                    sb.AppendLine($"x{i + 1} = {NumFormat.N3(solutionVector[i])}");
            }

            WriteToFile(filePath, sb.ToString(), append);
        }

        // ----------------- helpers -----------------

        private static void EnsureDirectory(string filePath)
        {
            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
        }

        private static void WriteToFile(string filePath, string content, bool append)
        {
            if (append && File.Exists(filePath))
                File.AppendAllText(filePath, content, Encoding.UTF8);
            else
                File.WriteAllText(filePath, content, Encoding.UTF8);
        }
    }
}
