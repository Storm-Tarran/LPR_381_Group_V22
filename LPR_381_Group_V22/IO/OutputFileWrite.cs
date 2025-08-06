using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using static LPR_381_Group_V22.IO.InputFileParser;
using static LPR_381_Group_V22.Utilities.CanonicalFormConverter;

namespace LPR_381_Group_V22.IO
{
    public static class OutputFileWrite
    {
        public static void WriteFullResults(string outputPath, string problemType, string solverUsed, List<double> objectiveCoefficients,
            List<Constraint> constraints, List<string> signRestrictions, List<string> iterationSnapshots, double finalZ, List<double> solutionVector)
        {
            string mainPath = AppDomain.CurrentDomain.BaseDirectory;
            string projectRoot = Directory.GetParent(mainPath).Parent.Parent.FullName;
            string fullPath = Path.Combine(projectRoot, outputPath);

            using (StreamWriter writer = new StreamWriter(fullPath))
            {
                writer.WriteLine("=== Solver Results ===");
                writer.WriteAsync($"Solver used: {problemType}");
                writer.WriteLine($"\nProblem Type: {solverUsed}");
                var objectiveFunction = new StringBuilder();
                objectiveFunction.Append("Objective Function: Z = ");
                for (int i = 0; i < objectiveCoefficients.Count; i++)
                {
                    double oCoeff = objectiveCoefficients[i];
                    objectiveFunction.Append($"{(oCoeff >= 0 ? "+ " : "- ")}{oCoeff}x{i + 1} ");
                }
                writer.WriteLine(objectiveFunction.ToString().Trim());
                writer.WriteLine("Constraints:");
                foreach(var constraint in constraints)
                {
                    for (int i = 0; i < constraint.Coefficients.Count; i++)
                    {
                        writer.Write($"{(constraint.Coefficients[i] >= 0 ? "+" : "")}{constraint.Coefficients[i]}x{i + 1} ");
                    }
                    writer.WriteLine($"{constraint.Relation} {constraint.RHS:0.000}");
                }
                writer.WriteLine("Sign Restrictions:");
                writer.WriteLine(string.Join(" ", signRestrictions));

                writer.WriteLine(CanonicalFormForFile(problemType, objectiveCoefficients, constraints, signRestrictions));

                writer.WriteLine("\n=== Iteration Snapshots ===");
                foreach (var snapshot in iterationSnapshots)
                {
                    writer.WriteLine(snapshot);
                    writer.WriteLine("-------------------------------------------------------------------------------------");
                }

                writer.WriteLine($"\nFinal Z value: {finalZ:0.000}");
                writer.WriteLine("Variable Solutions:");
                for (int i = 0; i < solutionVector.Count; i++)
                {
                    writer.WriteLine($"x{i + 1}: {solutionVector[i]:0.000}");
                }
            }
        }
    }
}
