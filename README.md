# Linear & Integer Programming Solver

## 📋 Project Overview
This is a Linear and Integer Programming Solver developed for the Linear Programming 381 (LPR381) course project. The solver is built as a C# Console Application (Visual Studio) and supports solving Linear Programming (LP) and Integer Programming (IP) models using a variety of algorithms, as seen below. The application allows you to input problem models via a text file, solve them using selected methods, and output the canonical form, table iterations, and solution results into a structured output file.

## 🧮 Algorithms Implemented:

    Primal Simplex Method

    Revised Primal Simplex Method

    Branch & Bound Simplex Method

    Cutting Plane Method

    Branch & Bound Knapsack Algorithm

## ▶️ How to Run
1. Open the solution file LPR_381_Group_V22.sln in Visual Studio.
2. Place your input file (e.g., sample_input.txt) inside the /data/ folder.
3. Build the project.
4. Run the application.
5. Use the console Menu-Driven Interface to:
   - Select solving algorithms.
   - View Canonical Forms.
   - Export results to an output file.

## 📝 Input File Format Example
1. `max +2 +3 +4`
2. `+1 +2 +3 <= 10`
3. `+3 +2 +1 >= 15`
4. `+ + +`
- First Line: Problem Type (max or min) followed by Objective Coefficients.
- Following Lines: Constraints (coefficients and signs and RHS).
- Last Line: Sign Restrictions (+ for >=) and (- for <=).

## ✅ Features Implemented
- Menu-Driven Console App 
- Input Parsing & Validation
- Canonical Form Display
- Primal & Revised Primal Simplex Solvers
- Branch & Bound Simplex Solver
- Cutting Plane Algorithm
- Branch & Bound Knapsack Solver
- Sensitivity Analysis & Duality Features
- Error Handling (Infeasible/Unbounded Cases)
- Output File Generation (Canonical Form & Iterations)

## 👥 Contributors
- `Storm Tarran 600995: Input/Output Handling, Canonical Form, Primal Simplex & Branch and Bound Simplex Solver`
- `Demica Smit 577875: Revised Simplex Method, Sensitivity Analysis`
- `Adrian Christopher Conradie 600548: Cutting Plane Solver & Dual Simplex Solver`
- `Leonard Bezuidenhout	578375: Knapsack Solver, Testing, Documentation`
