using LPR_381_Group_V22.IntegerProgramming;
using LPR_381_Group_V22.IO;
using LPR_381_Group_V22.Simplex;
using LPR_381_Group_V22.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LPR_381_Group_V22.SensitivityAnalysis;
using LPR_381_Group_V22.NonLinear;

namespace LPR_381_Group_V22
{
    internal class Program
    {
        
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            InputFileParser inputFileParser = new InputFileParser();
            bool fileLoaded = false;
            bool demo = false;

            while (!fileLoaded)
            {
                Console.Clear();
                Console.WriteLine("=== Linear & Integer Programming Solver ===");
                Console.WriteLine("Please enter the name of your file (eg. textfile.txt): ");
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
            bool isMaximization = inputFileParser.ProblemType?.Trim().ToLower() == "max";
            bool isMinimization = string.Equals(inputFileParser.ProblemType, "min", StringComparison.OrdinalIgnoreCase);


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
                Console.WriteLine("6. Solve a Non-Linear Problem");
                Console.WriteLine("7. Exit");
                Console.Write("Please select an option (1-6): ");
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

                        var objective = new List<double>(inputFileParser.ObjectiveCoefficients);
                        var constraints = inputFileParser.Constraints
                            .Select(c => new InputFileParser.Constraint(new List<double>(c.Coefficients), c.Relation, c.RHS))
                            .ToList();

                        CanonicalFormConverter.DisplayCanonicalForm(
                            inputFileParser.ProblemType,
                            inputFileParser.ObjectiveCoefficients,
                            inputFileParser.Constraints,
                            inputFileParser.SignRestrictions);

                        int numberConstraints = inputFileParser.ObjectiveCoefficients.Count;
                        int vecLength = inputFileParser.ObjectiveCoefficients.Count + 3;

                        for (int i = 0; i < numberConstraints; i++)
                        {
                            var coefficients = new List<double>(new double[vecLength]);
                            coefficients[i] = 1;
                            coefficients[vecLength - 2] = 1;
                            var constraint = new InputFileParser.Constraint(coefficients, "<=", 1); // assuming the relation is "<=" and RHS is 1
                            inputFileParser.Constraints.Add(constraint);
                        }

                        var primalSolver = new PrimalSimplexSolver(inputFileParser.ObjectiveCoefficients, inputFileParser.Constraints);

                        primalSolver.Solve();

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

                        // Initialize SensitivityAnalyzer
                        SensitivityAnalyzer sensitivityAnalyzer = new SensitivityAnalyzer(
                         primalSolver.GetFinalTableau(),
                         primalSolver.SolutionVector,
                         primalSolver.FinalZ,
                         primalSolver.BasicVariables);


                        bool backToMain = false;

                        while (!backToMain)
                        {
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
                            Console.WriteLine("13. Return to main menu");
                            Console.Write("Please select an option (1-13): ");
                            string choice2 = Console.ReadLine();


                            switch (choice2)
                            {


                                case "1":


                                    Console.WriteLine("=== Display Non-Basic Variable Range ===");
                                    sensitivityAnalyzer.DisplayRangeNonBasic();

                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();

                                    break;

                                case "2":

                                    Console.WriteLine("=== Change Non-Basic Variable ===");
                                    sensitivityAnalyzer.ChangeNonBasicReducedCost();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();

                                    break;

                                case "3":

                                    Console.WriteLine("=== Display Basic Variable Range ===");
                                    sensitivityAnalyzer.DisplayRangeBasic();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();

                                    break;

                                case "4":

                                    Console.WriteLine("=== Change Basic Variable ===");
                                    sensitivityAnalyzer.ChangeBasic();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;

                                case "5":

                                    Console.WriteLine("=== Display RHS Range ===");
                                    sensitivityAnalyzer.DisplayRangeRHS();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;

                                case "6":

                                    Console.WriteLine("=== Change RHS ===");
                                    sensitivityAnalyzer.ChangeRHS();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;

                                case "7":

                                    Console.WriteLine("=== Display Non-Basic Column Range ===");
                                    sensitivityAnalyzer.DisplayRangeNonBasicColumn();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;

                                case "8":

                                    Console.WriteLine("=== Change Non-Basic Column ===");
                                    sensitivityAnalyzer.ChangeNonBasicColumn();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;

                                case "9":

                                    Console.WriteLine("=== Add New Activity ===");
                                    sensitivityAnalyzer.AddNewActivity(); // this already re-optimizes if needed

                                    // pull the recalculated results
                                    var T = sensitivityAnalyzer.CurrentTableau;
                                    var Z = sensitivityAnalyzer.CurrentZ;
                                    var x = sensitivityAnalyzer.CurrentSolutionVector;

                                    Console.WriteLine($"Updated Z* = {Z:0.###}");
                                    for (int i = 0; i < Math.Min(x.Count, inputFileParser.ObjectiveCoefficients.Count); i++)
                                        Console.WriteLine($"x{i + 1} = {x[i]:0.###}");

                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;

                                case "10":

                                    Console.WriteLine("=== Add New Constraint ===");
                                    sensitivityAnalyzer.AddNewConstraint();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;

                                case "11":

                                    Console.WriteLine("=== Display Shadow Prices ===");
                                    sensitivityAnalyzer.DisplayShadowPrices();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;

                                case "12":

                                    Console.WriteLine("=== Perform Duality ===");
                                    sensitivityAnalyzer.PerformDuality();
                                    Console.WriteLine("Press Enter to continue...");
                                    Console.ReadLine();
                                    break;
                                case "13":

                                    backToMain = true;
                                    Console.Clear();
                                    Console.ReadKey();


                                    break;


                                default:
                                    Console.WriteLine("Invalid choice. Please select a valid option (1-6).");
                                    break;


                            }





                        }

                        break;

                    case "2":
                        {
                            Console.Clear();
                            Console.WriteLine("Solving with Revised Primal Simplex Algorithm...");

                            // Work on fresh copies so we don't mutate the parser's lists
                            objective = new List<double>(inputFileParser.ObjectiveCoefficients);
                            constraints = inputFileParser.Constraints
                                .Select(c => new InputFileParser.Constraint(new List<double>(c.Coefficients), c.Relation, c.RHS))
                                .ToList();

                            // show once on screen
                            CanonicalFormConverter.DisplayCanonicalForm(
                                inputFileParser.ProblemType,
                                objective,
                                constraints,
                                inputFileParser.SignRestrictions);



                            // Determine minimization correctly (default: MAX if null/unknown)
                            bool isMin = string.Equals(inputFileParser.ProblemType, "min", StringComparison.OrdinalIgnoreCase);


                            AddUpperBoundConstraints(objective.Count, inputFileParser.SignRestrictions, constraints);


                            var revisedSolver = new RevisedPrimalSimplexSolver(objective, constraints, isMin);

                            try
                            {
                                revisedSolver.Solve();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("\n✖ Error while solving with Revised Simplex:");
                                Console.WriteLine(ex.Message);
                                Console.WriteLine("\nPress any key to return to the main menu...");
                                Console.ReadKey();
                                break;
                            }

                            
                            if (revisedSolver.IterationSnapshots.Count > 0)
                            {
                                Console.WriteLine("\n=== Iterations ===");
                                foreach (var snap in revisedSolver.IterationSnapshots)
                                {
                                    Console.WriteLine(snap);
                                    Console.WriteLine("Press any key to see next iteration...");
                                    Console.ReadKey();
                                }
                            }

                            // Results (3-decimals only when needed)
                            Console.WriteLine($"\nOptimal Objective Value (Z*): {NumFormat.N3(revisedSolver.FinalZ)}");
                            Console.WriteLine("Decision Variables:");
                            for (int i = 0; i < revisedSolver.SolutionVector.Count; i++)
                                Console.WriteLine($"x{i + 1} = {NumFormat.N3(revisedSolver.SolutionVector[i])}");

                            // Persist results
                            solverUsed = "Revised Primal Simplex Algorithm";
                            OutputFileWrite.WriteFullResults(
                                 "data/output_results.txt",
                                 solverUsed: "Revised Primal Simplex Algorithm",
                                 problemType: inputFileParser.ProblemType, // <- IMPORTANT: pass the actual "min"/"max"
                                 objectiveCoefficients: objective,
                                 constraints: constraints,
                                 signRestrictions: inputFileParser.SignRestrictions,
                                 iterationSnapshots: revisedSolver.IterationSnapshots,
                                 finalZ: revisedSolver.FinalZ,
                                 solutionVector: revisedSolver.SolutionVector
                             );

                            Console.WriteLine("\nAll results have been saved to 'output_results.txt'.");
                            break;
                        }

                    case "3":
                        Console.Clear();

                        // --- Capture everything we print in this case (and still show it on screen) ---
                        var oldOut = Console.Out;
                        var buffer = new StringWriter();
                        Console.SetOut(new BranchBoundSimplexSolver.TeeTextWriter(oldOut, buffer)); 

                        Console.WriteLine("Solving with Branch and Bound Simplex Algorithm...");

                        CanonicalFormConverter.DisplayCanonicalForm(
                            inputFileParser.ProblemType,
                            inputFileParser.ObjectiveCoefficients,
                            inputFileParser.Constraints,
                            inputFileParser.SignRestrictions);

                        int numConstraints = inputFileParser.ObjectiveCoefficients.Count;
                        int vectorLength = inputFileParser.ObjectiveCoefficients.Count + 3;

                        for (int i = 0; i < numConstraints; i++)
                        {
                            var coefficients = new List<double>(new double[vectorLength]);
                            coefficients[i] = 1;
                            coefficients[vectorLength - 2] = 1;
                            var constraint = new InputFileParser.Constraint(coefficients, "<=", 1);
                            inputFileParser.Constraints.Add(constraint);
                        }

                        // Solve LP (primal) first
                        var primal = new PrimalSimplexSolver(inputFileParser.ObjectiveCoefficients, inputFileParser.Constraints);
                        primal.Solve();

                        // Solve integer (B&B)
                        var (x_bb, z_bb) = BranchAndBoundAdapter.SolveFromPrimal(primal, enablePruning: false, isMin: false);

                        Console.WriteLine("\n=== Branch & Bound Result ===");
                        Console.WriteLine($"Z* = {z_bb:0.###}");
                        for (int i = 0; i < x_bb.Count; i++)
                            Console.WriteLine($"x{i + 1} = {x_bb[i]:0.###}");

                        Console.SetOut(oldOut);

                        // Build a single "snapshot" with everything printed
                        var snapshots = new List<string> { buffer.ToString() };

                        string solverUsedLabel = "Branch and Bound Simplex Algorithm";
                        OutputFileWrite.WriteSnapshotsOnly(
                            "data/output_results.txt", 
                            solverUsedLabel,                    
                            snapshots,                    
                            z_bb,                      
                            x_bb,
                            append: false
                        );

                        Console.WriteLine("----------------------------------------------");
                        Console.WriteLine("\nAll results have been saved to 'output_results.txt'.");
                        Console.WriteLine("Press any key to return to the main menu...");
                        Console.ReadKey();
                        break;

                    case "4":
                        Console.Clear();
                        Console.WriteLine("Solving with Cutting Plane Algorithm...");
                        CanonicalFormConverter.DisplayCanonicalForm(
                            inputFileParser.ProblemType,
                            inputFileParser.ObjectiveCoefficients,
                            inputFileParser.Constraints,
                            inputFileParser.SignRestrictions);

                        

                        break;

                    case "5":
                        Console.WriteLine("Solving with Branch and Bound Knapsack Algorithm...");

                        int capacity = 40;
                        int[] weights = { 11, 8, 6, 14, 10, 10 };
                        int[] values = { 2, 3, 3, 5, 2, 4 };

                        Console.WriteLine("Knapsack Problem");
                        Console.WriteLine($"Capacity: {capacity}");
                        Console.WriteLine("Items (Value, Weight):");
                        for (int i = 0; i < values.Length; i++)
                            Console.WriteLine($"  Item {i + 1}: Value={values[i]}, Weight={weights[i]}");
                        Console.WriteLine();

                        var solver = new KnapsackBranchBoundSolver(
                            capacity,
                            weights.Select(w => (double)w).ToArray(),
                            values.Select(v => (double)v).ToArray()
                        );

                        double branchBoundResult = solver.Solve();

                        Console.WriteLine("=== Branch and Bound Detailed Steps ===");
                        solver.PrintIterations();

                        // Show chosen items mapped back to original indices
                        var chosen = solver.GetSelectedItemsOriginal();
                        Console.WriteLine("\nChosen items (original numbering):");
                        int totalW = 0;
                        foreach (var it in chosen)
                        {
                            Console.WriteLine($"  x{it.Id + 1} = 1  (Value={it.Value}, Weight={it.Weight})");
                            totalW += (int)it.Weight;
                        }
                        Console.WriteLine($"Total Weight = {totalW}");
                        Console.WriteLine($"Branch & Bound Best Value Z* = {branchBoundResult}");

                        Console.WriteLine("\n=== Comparison with Dynamic Programming ===");
                        double dp = KnapsackBranchBoundSolver.Solve(capacity, weights, values);
                        Console.WriteLine($"Dynamic Programming Result: {dp}");
                        Console.WriteLine($"Results Match: {Math.Abs(dp - branchBoundResult) < 1e-6}");
                        break;

                    case "6":
                        Console.Clear();
                        Console.WriteLine("Solving a Non-Linear Problem...");
                        var bonusQuestion = new BonusQuestion();
                        Console.WriteLine("How would you like to solve this problem?");
                        Console.WriteLine("1. Minimization");
                        //string intput1 = Console.ReadLine();
                        Console.WriteLine("2. Maximization");
                        string input = Console.ReadLine();
                        if(input == "1")
                        {
                            bonusQuestion.SolveMin();

                        }
                        else
                        {
                            bonusQuestion.SolveMax();
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

        private static void AddUpperBoundConstraints(
    int n,
    List<string> signRestrictions,
    List<InputFileParser.Constraint> constraints)
        {
            if (signRestrictions == null || signRestrictions.Count == 0) return;

            // Assume signRestrictions[j] corresponds to x{j+1} like "0 ≤ x1 ≤ 1" or "bin"
            for (int j = 0; j < n; j++)
            {
                var sr = signRestrictions[Math.Min(j, signRestrictions.Count - 1)] ?? "";
                var srNoSpace = sr.Replace(" ", "");

                bool isBinary = srNoSpace.IndexOf("bin", StringComparison.OrdinalIgnoreCase) >= 0;
                bool hasUpperOne = srNoSpace.Contains("≤1") || srNoSpace.Contains("<=1");

                // If you only care about 0..1 and bin, this is enough:
                if (isBinary || hasUpperOne)
                {
                    var coeffs = Enumerable.Repeat(0.0, n).ToList();
                    coeffs[j] = 1.0;
                    constraints.Add(new InputFileParser.Constraint(coeffs, "<=", 1.0));
                }
            }
        }
    }


}
