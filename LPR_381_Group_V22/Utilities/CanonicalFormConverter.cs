using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LPR_381_Group_V22.IO.InputFileParser;

namespace LPR_381_Group_V22.Utilities
{
    public static class CanonicalFormConverter
    {


        // FOR CONSOLE OUTPUT...im lazy to go change this whole thing, so im just adding
        public static void DisplayCanonicalForm(string problemType, List<double> objectiveCoefficients,
            List<Constraint> constraints, List<string> signRestrictions)
        {
            Console.WriteLine("\n=== Canonical Form ===");

            // Print Objective Function
            Console.Write($"{problemType.ToUpper()} Z ");
            for (int i = 0; i < objectiveCoefficients.Count; i++)
            {
                double oCoeficients = objectiveCoefficients[i] * -1; // Negate for maximization problems
                Console.Write($"{FormatCoeff(oCoeficients)}x{i + 1} ");
            }
            Console.WriteLine("= 0\n");

            // Print Constraints with Slack Variables
            for (int i = 0; i < constraints.Count; i++)
            {
                var constraint = constraints[i];
                for (int j = 0; j < constraint.Coefficients.Count; j++)
                {
                    Console.Write($"{FormatCoeff(constraint.Coefficients[j])}x{j + 1} ");
                }

                // Add Slack/Surplus Variable Display
                Console.Write($"+ S{i + 1} ");

                Console.WriteLine($"= {constraint.RHS}");
            }
            Console.WriteLine();

            // Print Sign Restrictions
            Console.Write("Sign Restrictions: ");
            for (int i = 0; i < signRestrictions.Count; i++)
            {
                Console.Write($"x{i + 1}: {signRestrictions[i]} ");
            }
            Console.WriteLine("\n======================\n");
        }

        // FOR FILE OUTPUT
        public static string CanonicalFormForFile(string problemType, List<double> objectiveCoefficients,
            List<Constraint> constraints, List<string> signRestrictions)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine("\n=== Canonical Form ===");
            stringBuilder.Append($"Z ");
            for (int i = 0; i < objectiveCoefficients.Count; i++)
            {
                double oCoeficients = objectiveCoefficients[i] * -1; // Negate for maximization problems
                stringBuilder.Append($"{FormatCoeff(oCoeficients)}x{i + 1} ");
            }
            stringBuilder.Append("= 0\n");

            // Print Constraints with Slack Variables
            for (int i = 0; i < constraints.Count; i++)
            {
                var constraint = constraints[i];
                for (int j = 0; j < constraint.Coefficients.Count; j++)
                {
                    stringBuilder.Append($"{FormatCoeff(constraint.Coefficients[j])}x{j + 1} ");
                }

                // Add Slack/Surplus Variable Display
                stringBuilder.Append($"+ S{i + 1} ");

                stringBuilder.Append($"= {constraint.RHS}\n");
            }

            // Print Sign Restrictions
            stringBuilder.Append("\nSign Restrictions: ");
            for (int i = 0; i < signRestrictions.Count; i++)
            {
                stringBuilder.Append($"x{i + 1}: {signRestrictions[i]} ");
            }
            stringBuilder.AppendLine("\n======================\n");

            return stringBuilder.ToString();
        }

        private static string FormatCoeff(double coeff)
        {
            return coeff >= 0 ? $"+ {coeff}" : coeff.ToString();
        }
    }
}
