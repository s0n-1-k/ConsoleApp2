using MathExpressionsLab;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MathExpressionsLab
{
    [ExcludeFromCodeCoverage]
    internal class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            var x = new Variable("x"); var y = new Variable("y");
            var c = new Constant(-3);
            var expr1 = (x - 4) * (3 * x + y * y) / 5;
            var expr2 = -(5 - 3 * c) * Expr.Sqrt(16 + c * c);
            var values = new Dictionary<string, double>
            {
                ["x"] = 1,
                ["y"] = 2
            };
            PrintExprReport("expr1", expr1, values, "(x - 4) * (3 * x + y * y) / 5", -4.2);
            PrintExprReport("expr2", expr2, values, "-(5 - 3 * 3) * Sqrt(16 + 3 * 3)", 20.0);


            static void PrintExprReport(string name, Expr expr, IReadOnlyDictionary<string, double> values,
            string expectedToString, double expectedVal)
            {
                Console.WriteLine($"\n--- {name} ---");
                Console.WriteLine($"ToString():         {expr}");
                Console.WriteLine($"Expected:           {expectedToString}");
                Console.WriteLine($"Variables:          [{string.Join(", ", expr.Variables)}]");
                Console.WriteLine($"IsConstant:         {expr.IsConstant}");
                Console.WriteLine($"IsPolynomial:       {expr.IsPolynomial}");
                Console.WriteLine($"PolynomialDegree:   {expr.PolynomialDegree}");

                double computedVal = expr.Compute(values);
                Console.WriteLine($"Compute(values):    {computedVal:F4}");
                Console.WriteLine($"Expected Val:       {expectedVal:F4}");

                if (Math.Abs(computedVal - expectedVal) > 1e-9)
                {
                    Console.WriteLine("ERROR: Value mismatch!");
                }
            }
        }
    }
}