using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LPR_381_Group_V22.NonLinear
{
    public class BonusQuestion
    {
        public static readonly double ratio = (Math.Sqrt(5.0) - 1.0) / 2.0; // (sqrt(5) - 1) / 2

        public sealed class GoldenIteration
        {
            public int Iteration { get; set; }
            public double xLower { get; set; }
            public double xHigher { get; set; }
            public double Distance { get; set; }
            public double X1 { get; set; }
            public double X2 { get; set; }
            public double f_X1 { get; set; }
            public double f_X2 { get; set; }
            public double IntervalLength_Gap { get; set; }
            public string Choice { get; set; } = ""; 

        }

        public static class GoldenTable
        {
            public static string FormatGoldenTable(IReadOnlyList<GoldenIteration> rows, int decimals = 10)
            {
                var sb = new StringBuilder();

                string H(string s, int w) => s.PadRight(w);
                string F(double v) => Math.Round(v, decimals).ToString($"F{decimals}");

                // Header
                sb.AppendLine(
                    H("Iter", 1) + H("xLow", 14) + H("xHigh", 14) +
                    H("Distance", 14) + H("x1", 14) + H("x2", 14) + H("f(x1)", 14) +
                    H("f(x2)", 14) + H("Interval", 14) +
                    H("Decision", 24)
                );
                sb.AppendLine(new string('-', 6 + 14 * 8 + 24));

                // Rows
                foreach (var r in rows)
                {
                    sb.AppendLine(
                        H(r.Iteration.ToString(), 6) +
                        H(F(r.xLower), 14) +
                        H(F(r.xHigher), 14) +
                        H(F(r.Distance), 14) +
                        H(F(r.X1), 14) +
                        H(F(r.X2), 14) +
                        H(F(r.f_X1), 14) +
                        H(F(r.f_X2), 14) +
                        H(F(r.IntervalLength_Gap), 14) +
                        H(r.Choice, 24)
                    );

                }
                return sb.ToString();
            }

        }

        public double Solve()
        {
            Func<double, double> f = x => x * x;

            double xLow = 0; //Start Interval
            double xHigh = 2; //End Interval

            var rows = new List<GoldenIteration>();

            for (int i = 0; i <= 40; i++)
            {
                double firstlow = xLow, firstHigh = xHigh;

                double distance = ratio * (xHigh - xLow); // ratio * (xHigh - xLow)
                double x1 = xLow + distance; // xLow + distance
                double x2 = xHigh - distance; // xHigh - distance
                double f_x1 = f(x1);
                double f_x2 = f(x2);
                double gap = xHigh - xLow;
                string choice;

                if (f_x1 > f_x2)
                {
                    choice = $"Keep [{xLow:F3}, {x2:F3}]";
                    xHigh = x1;
                }
                else
                {
                    choice = $"Keep [{xLow:F3}, {x2:F3}]";
                    xLow = x2;
                }

                rows.Add(new GoldenIteration
                {
                    Iteration = i,
                    xLower = firstlow,
                    xHigher = firstHigh,
                    Distance = distance,
                    X1 = x1,
                    X2 = x2,
                    f_X1 = f_x1,
                    f_X2 = f_x2,
                    IntervalLength_Gap = gap,
                    Choice = choice
                });

                if(gap <= 0.05)
                    break;
            }

            Console.WriteLine("\nGolden-Section Search: f(x) = x^2, start [0, 2]");
            Console.WriteLine(GoldenTable.FormatGoldenTable(rows, 10));
            double optimal = (xHigh + xLow)/2;
            double formula = Math.Pow(optimal,2);
            Console.WriteLine($"The optimal value is: {optimal} and formula value is: {formula}");
            Console.WriteLine("Press any key to return to the main menu...");
            Console.ReadKey();

            return 0.5 * (xHigh + xLow); // Return the midpoint of the final interval as the estimated minimum
        }

    }
}
