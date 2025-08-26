using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace LPR_381_Group_V22.IO
{
    public class InputFileParser
    {
        public string ProblemType { get; set; } //For min or max problem
        public List<double> ObjectiveCoefficients { get; set; } = new List<double>(); //For the objective function coefficients
        public List<Constraint> Constraints { get; set; } = new List<Constraint>(); //For the constraints coefficients
        public List<string> SignRestrictions { get; set; } = new List<string>(); //For the sign restrictions of the constraints


        //Reading the input file and parsing the data
        public void ReadInputFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine("Sorry, we can't find your file, please check it's in the right folser");
                return;
            }

            string[] linesInFile = File.ReadAllLines(filePath);

            // Checks if the file has at least 3 lines (objective, constraints, sign restrictions)
            if (linesInFile.Length < 3)
            {
                Console.WriteLine("The input file is not formatted correctly.");
                return;
            }

            string[] objectiveLine = linesInFile[0].Trim().Split(' ');
            ProblemType = objectiveLine[0].ToLower();

            for (int i = 1; i < objectiveLine.Length; i++)
            {
                double objectiveCoefficient = double.Parse(objectiveLine[i], System.Globalization.CultureInfo.InvariantCulture);
                ObjectiveCoefficients.Add(objectiveCoefficient);
            }

            for (int i = 1; i < linesInFile.Length - 1; i++)
            {
                string[] constraintParts = linesInFile[i].Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                List<double> coefficientConstraint = new List<double>();

                // Parse coefficients for the constraint
                for (int j = 0; j < ObjectiveCoefficients.Count; j++)
                {
                    double coefficients = double.Parse(constraintParts[j], System.Globalization.CultureInfo.InvariantCulture);
                    coefficientConstraint.Add(coefficients);
                }
                // Parse the relation and right-hand side value
                string relation = constraintParts[ObjectiveCoefficients.Count];
                double rhs = double.Parse(constraintParts[ObjectiveCoefficients.Count + 1], System.Globalization.CultureInfo.InvariantCulture);

                // Add the constraint to the list
                Constraints.Add(new Constraint(coefficientConstraint, relation, rhs));
            }

            string[] signRestrictionsLine = linesInFile[linesInFile.Length - 1].Trim().Split(' ');
            SignRestrictions.AddRange(signRestrictionsLine);

            Console.WriteLine("Your file was read and is in the correct format!");
        }

        public class Constraint
        {
            public List<double> Coefficients { get; }
            public string Relation { get; }
            public double RHS { get; }

            public Constraint(List<double> coefficients, string relation, double rhs)
            {
                Coefficients = coefficients;
                Relation = relation;
                RHS = rhs;
            }
        }
    }
}
