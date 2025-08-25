using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static LPR_381_Group_V22.IO.InputFileParser;

namespace LPR_381_Group_V22.Utilities
{
    public static class CanonicalFormConverter
    {
        private static List<Constraint> AugmentWithSignRestrictions(
            List<Constraint> baseConstraints,
            List<string> signRestrictions,
            int numVars)
        {
            var augmented = new List<Constraint>(baseConstraints);

            List<double> Unit(int k)
            {
                var v = new double[numVars];
                v[k] = 1.0;
                return new List<double>(v);
            }

            for (int i = 0; i < Math.Min(numVars, signRestrictions?.Count ?? 0); i++)
            {
                var tok = (signRestrictions[i] ?? "").Trim().ToLowerInvariant();
                switch (tok)
                {
                    case "bin":
                        // 0 <= x_i <= 1
                        augmented.Add(new Constraint(Unit(i), "<=", 1.0)); 
                        break;
                    case ">=0":
                        augmented.Add(new Constraint(Unit(i).Select(x => -x).ToList(), "<=", 0.0)); 
                        break;
                    case "<=0":
                        augmented.Add(new Constraint(Unit(i), "<=", 0.0)); 
                        break;
                    case "free":
                        // no implied bounds
                        break;
                }
            }
            return augmented;
        }

        // CONSOLE OUTPUT
        public static void DisplayCanonicalForm(
            string problemType,
            List<double> objectiveCoefficients,
            List<Constraint> constraints,
            List<string> signRestrictions)
        {
            // IMPORTANT: include implied rows
            var allConstraints = AugmentWithSignRestrictions(constraints, signRestrictions, objectiveCoefficients.Count);

            Console.WriteLine("\n=== Canonical Form ===");

            // Objective (your style prints max as negative on LHS = 0)
            Console.Write($"{problemType.ToUpper()} Z ");
            for (int i = 0; i < objectiveCoefficients.Count; i++)
            {
                double c = -objectiveCoefficients[i]; // keep your convention
                Console.Write($"{FormatCoeff(c)}x{i + 1} ");
            }
            Console.WriteLine("= 0\n");

            // Constraints (+ proper slack/surplus display by relation)
            for (int i = 0; i < allConstraints.Count; i++)
            {
                var c = allConstraints[i];
                for (int j = 0; j < c.Coefficients.Count; j++)
                    Console.Write($"{FormatCoeff(c.Coefficients[j])}x{j + 1} ");

                // show slack/surplus symbolically (purely for display)
                if (c.Relation == "<=") Console.Write($"+ t{i + 1} ");
                else if (c.Relation == ">=") Console.Write($"- t{i + 1} ");
                else /* "=" */ Console.Write($"+ t{i + 1} ");

                Console.WriteLine($"= {c.RHS}");
            }
            Console.WriteLine();

            Console.Write("Sign Restrictions: ");
            for (int i = 0; i < signRestrictions.Count; i++)
                Console.Write($"x{i + 1}: {signRestrictions[i]} ");
            Console.WriteLine("\n======================\n");
        }

        // FILE OUTPUT (same idea; include implied rows)
        public static string CanonicalFormForFile(
            string problemType,
            List<double> objectiveCoefficients,
            List<Constraint> constraints,
            List<string> signRestrictions)
        {
            var allConstraints = AugmentWithSignRestrictions(constraints, signRestrictions, objectiveCoefficients.Count);
            var sb = new StringBuilder();

            sb.AppendLine("\n=== Canonical Form ===");
            sb.Append("Z ");
            for (int i = 0; i < objectiveCoefficients.Count; i++)
            {
                double c = -objectiveCoefficients[i];
                sb.Append($"{FormatCoeff(c)}x{i + 1} ");
            }
            sb.Append("= 0\n");

            for (int i = 0; i < allConstraints.Count; i++)
            {
                var c = allConstraints[i];
                for (int j = 0; j < c.Coefficients.Count; j++)
                    sb.Append($"{FormatCoeff(c.Coefficients[j])}x{j + 1} ");

                if (c.Relation == "<=") sb.Append($"+ t{i + 1} ");
                else if (c.Relation == ">=") sb.Append($"- t{i + 1} ");
                else sb.Append($"+ t{i + 1} ");

                sb.Append($"= {c.RHS}\n");
            }

            sb.Append("\nSign Restrictions: ");
            for (int i = 0; i < signRestrictions.Count; i++)
                sb.Append($"x{i + 1}: {signRestrictions[i]} ");
            sb.AppendLine("\n======================\n");

            return sb.ToString();
        }

        private static string FormatCoeff(double coeff) => coeff >= 0 ? $"+ {coeff}" : coeff.ToString();
    }
}

