using LPR_381_Group_V22.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LPR_381_Group_V22.Simplex;
using LPR_381_Group_V22.IntegerProgramming;
using LPR_381_Group_V22.Utilities;
using System.Linq.Expressions;
using System.Diagnostics;

namespace LPR_381_Group_V22
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            InputFileParser inputFileParser = new InputFileParser();
            bool fileLoaded = false;
            bool demo = false;

            while (!fileLoaded)
            {
                Console.Clear();
                Console.WriteLine("=== Linear & Integer Programming Solver ===");
                Console.WriteLine("Please enter the name of your file (eg. Myquestionfile.txt): ");
                string filePath = Console.ReadLine();
                string fullPath=null;
                if (string.IsNullOrEmpty(filePath))
                { 
                    demo= true;
                    filePath = "textfile.txt";
                    string mainPath = AppDomain.CurrentDomain.BaseDirectory;
                    string projectRoot = Directory.GetParent(mainPath).Parent.Parent.FullName;
                    fullPath = Path.Combine(projectRoot, "TextFile", filePath);
                }
                else
                {
                // Checking in the data folder, not in the root folder
                string mainPath = AppDomain.CurrentDomain.BaseDirectory;
                string projectRoot = Directory.GetParent(mainPath).Parent.Parent.FullName;
                fullPath = Path.Combine(projectRoot, "data", filePath);
                }

                if (!demo)
                {
                    inputFileParser.ReadInputFile(fullPath);
                    if (inputFileParser.ProblemType != null)
                    {
                        fileLoaded = true;
                        Console.WriteLine("File loaded successfully.");
                    }
                    else
                    {
                        Console.WriteLine("Error reading file. Please ensure the file is formatted correctly and try again.");
                        Console.WriteLine("Press any key to continue...");
                        Console.ReadKey();
                    }
                }
                else
                {
                    fileLoaded = true;
                }
            }
            
            string solverUsed = "";
            bool isMinimization = inputFileParser.ProblemType?.ToLower() == "min" || inputFileParser.ProblemType?.ToLower() == null;
            
            // This is the main menu
            bool exit = false;
            while (!exit)
            {
                Console.Clear();
                Console.WriteLine("=== Main Menu ===");
                Console.WriteLine("1. Solve with Primal Simplex Algorithm");
                Console.WriteLine("2. Solve with Revised Primal Simplex Algorithm");
                Console.WriteLine("3. Solve with Branch and Bound Simplex Algorithm");
                Console.WriteLine("4. Solve with Cutting Plane Algorithm");
                Console.WriteLine("5. Solve with Branch and Bound Knapsack Algorithm");
                Console.WriteLine("6. Sensitivity analysis");
                Console.WriteLine("7. Exit");
                Console.Write("Please select an option (1-7): ");
                string choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        if (isMinimization)
                        {
                            Console.WriteLine("\n⚠️ WARNING: This is a Minimization Problem.");
                            Console.WriteLine("Please use Option 2 (Revised Simplex Method) instead for better results!");
                            Console.WriteLine("Press any key to return to menu...");
                            Console.ReadKey();
                            break;
                        }
                        Console.Clear();
                        Console.WriteLine("Solving with Primal Simplex Algorithm...");

                        CanonicalFormConverter.DisplayCanonicalForm(
                            inputFileParser.ProblemType,
                            inputFileParser.ObjectiveCoefficients,
                            inputFileParser.Constraints,
                            inputFileParser.SignRestrictions);

                        PrimalSimplexSolver primalSolver = new PrimalSimplexSolver(inputFileParser.ObjectiveCoefficients, inputFileParser.Constraints);

                        primalSolver.Solve();

                        foreach (var snapshot in primalSolver.IterationSnapshots)
                        {
                            Console.WriteLine(snapshot);
                            Console.WriteLine("Press any key to see next iteration.");
                            Console.ReadKey();
                        }

                        Console.WriteLine($"Final Z value: {primalSolver.FinalZ}");
                        Console.WriteLine("Vaiable Solutions:");
                        for (int i = 0; i < primalSolver.SolutionVector.Count; i++)
                        {
                            Console.WriteLine($"x{i + 1}: {primalSolver.SolutionVector[i]}");
                        }

                        solverUsed = "Primal Simplex Algorithm";
                        OutputFileWrite.WriteFullResults(
                            "data/output_results.txt",
                            solverUsed,
                            inputFileParser.ProblemType,
                            inputFileParser.ObjectiveCoefficients,
                            inputFileParser.Constraints,
                            inputFileParser.SignRestrictions,
                            primalSolver.IterationSnapshots,
                            primalSolver.FinalZ,
                            primalSolver.SolutionVector);

                        Console.WriteLine("\nAll results have been saved to 'output_results.txt'.");
                        Console.ReadKey();
                        break;

                    case "2":
                        Console.WriteLine("Solving with Revised Primal Simplex Algorithm...");
                        break;

                    case "3":
                        Console.WriteLine("Solving with Branch and Bound Simplex Algorithm...");
                        break;

                    case "4":
                        Console.WriteLine("Solving with Cutting Plane Algorithm...");
                        var cuttingPlane = new CuttingPlaneSolver();
                        //get objrow and constraints from primal simplex
                        //cuttingPlane.CuttingPlaneSolution(objectiveRowtemp, constraintRowstemp);

                        break;

                    case "5":
                        Console.WriteLine("Solving with Branch and Bound Knapsack Algorithm...");
                        break;
                    case "6":
                        Console.Clear();
                        Console.WriteLine("Sensitivity Analysis");
                        Console.WriteLine("1. Display the range of a selected Non-Basic Variable.");
                        Console.WriteLine("2. Change a non-basic variable");
                        Console.WriteLine("3. Display the range of a selected Basic Variable.");
                        Console.WriteLine("4. Change a basic variable");
                        Console.WriteLine("5. Display the range of a selected constraint right-hand-side value.");
                        Console.WriteLine("6. Change a selected constraint right-hand-side value. ");
                        Console.WriteLine("7. Display the range of a selected variable in a Non-Basic Variable column. ");
                        Console.WriteLine("8. Change a selected variable in a Non-Basic Variable column");
                        Console.WriteLine("9. Add a new activity to an optimal solution.");
                        Console.WriteLine("10. Add a new constraint to an optimal solution. ");
                        Console.WriteLine("11. Display the shadow prices. ");
                        Console.WriteLine("12. Duality");
                        Console.Write("Please select an option (1-12): ");
                        string choice2 = Console.ReadLine();
                        switch (choice2)
                        {


                            case "1":
                                Console.Clear();
                                break;

                            case "2":
                                Console.Clear();
                                break;

                            case "3":
                                Console.Clear();
                                break;

                            case "4":
                                Console.Clear();
                                break;

                            case "5":
                                Console.Clear();
                                break;

                            case "6":
                                Console.Clear();
                                break;

                            case "7":
                                Console.Clear();
                                break;

                            case "8":
                                Console.Clear();
                                break;

                            case "9":
                                Console.Clear();
                                break;

                            case "10":
                                Console.Clear();
                                break;

                            case "11":
                                Console.Clear();
                                break;

                            case "12":
                                Console.Clear();
                                break;

                           
                            default:
                                Console.WriteLine("Invalid choice. Please select a valid option (1-6).");
                                break;


                            }
                            break;

                    case "7":
                        exit = true;
                        Console.WriteLine("Exiting the application. Goodbye!");
                        break;

                    default:
                        Console.WriteLine("Invalid choice. Please select a valid option (1-6).");
                        break;
                }
                if (!exit)
                {
                    Console.WriteLine("Press any key to return to the main menu...");
                    Console.ReadKey();
                }
            }
        }
    }
}
