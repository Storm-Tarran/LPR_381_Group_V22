using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LPR_381_Group_V22.Utilities
{
    public static class TableIterationFormater
    {
        //// nice numeric formatting (kill -0, up to 3 dp)
        //private static string F(double v)
        //{
        //    if (Math.Abs(v) < 1e-12) v = 0;
        //    double r = Math.Round(v, 3);
        //    return (r % 1 == 0) ? ((long)r).ToString() : r.ToString("0.###");
        //}

        public static string Format(double[,] tab, int numOriginalVars, string title) =>
        Format(tab, numOriginalVars, title, null);

        public static string Format(double[,] tab, int numOriginalVars, string title, IReadOnlyList<string> rowLabels)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"\n{title}:");
            sb.AppendLine(new string('-', 80));

            int rows = tab.GetLength(0);
            int cols = tab.GetLength(1);

            sb.Append("Table\t");
            for (int j = 0; j < numOriginalVars; j++) sb.Append($"x{j + 1}\t");
            for (int j = numOriginalVars; j < cols - 1; j++) sb.Append($"t{j - numOriginalVars + 1}\t");
            sb.AppendLine("RHS");

            sb.Append("Z\t");
            for (int j = 0; j < cols; j++) sb.Append($"{tab[0, j]:F3}\t");
            sb.AppendLine();

            for (int i = 1; i < rows; i++)
            {
                var label = (rowLabels != null && rowLabels.Count >= i) ? rowLabels[i - 1] : $"{i}";
                sb.Append(label + "\t");
                for (int j = 0; j < cols; j++) sb.Append($"{tab[i, j]:F3}\t");
                sb.AppendLine();
            }
            return sb.ToString();
        }

    }
}
