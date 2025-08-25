using LPR_381_Group_V22.Simplex;
using System;
using System.Collections.Generic;

namespace LPR_381_Group_V22.IntegerProgramming
{
    public static class BranchAndBoundAdapter
    {
        public static (List<double> x, double z) SolveFromPrimal(PrimalSimplexSolver primal, bool enablePruning = false, bool isMin = false)
        {
            if(primal.FinalTableau == null)
            {
                throw new InvalidOperationException("Primal simplex has not been solved yet.");
            }

            var finalTable = Convert(primal.FinalTableau);
            var tableAux = new List<List<List<double>>> { finalTable};
            var bb = new BranchBoundSimplexSolver.BranchAndBound();

            bb.SetNumVars(primal.SolutionVector?.Count ?? InferNumVariables(finalTable));
            
            var (x, z) = bb.ExecuteBranchAndBound(tableAux, enablePruning);
            return (x ?? new List<double>(), z);
        }

        private static int InferNumVariables(List<List<double>> table)
        {
            return Math.Max(1, table[0].Count - 1);
        }

        private static List<List<double>> Convert(double[,] tableau)
        {
            int rows = tableau.GetLength(0);
            int cols = tableau.GetLength(1);
            var result = new List<List<double>>(rows);
            for (int i = 0; i < rows; i++)
            {
                var row = new List<double>(cols);
                for (int j = 0; j < cols; j++)
                {
                    row.Add(tableau[i, j]);
                }
                result.Add(row);
            }
            return result;
        }

     

      
    }
}
