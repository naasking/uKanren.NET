using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uKanren;
using Sasa;

namespace KanrenTests
{
    class Program
    {
        static void Main(string[] args)
        {
            var x = Simple().Search(Kanren.EmptyState);
            Console.WriteLine("\r\nSimple:");
            Print(x);

            var s = Simple2().Search(Kanren.EmptyState);
            Console.WriteLine("\r\nSimple2:");
            Print(s);

            var y = SimpleConj().Search(Kanren.EmptyState);
            Console.WriteLine("\r\nSimpleConj:");
            Print(y);

            var fv = Kanren.Exists(Fives);
            Console.WriteLine("\r\nFives:");
            Print(fv.Search(Kanren.EmptyState));

            var oan = Kanren.Exists(OneAndNine);
            Console.WriteLine("\r\nOne and Nine:");
            Print(oan.Search(Kanren.EmptyState));

            var fs = FivesAndSixes();
            Console.WriteLine("\r\nFives and Sixes:");
            Print(fs.Search(Kanren.EmptyState));

            var fx = FivesXorSixes();
            Console.WriteLine("\r\nFives xor Sixes:");
            Print(fx.Search(Kanren.EmptyState));

            var a = Array();
            Console.WriteLine("\r\nArray:");
            Print(a.Search(Kanren.EmptyState));

            var n = NestedArray();
            Console.WriteLine("\r\nNestedArray:");
            Print(n.Search(Kanren.EmptyState));

            var ae = ArrayEquality();
            Console.WriteLine("\r\nArrayEquality:");
            Print(ae.Search(Kanren.EmptyState));

            var ad = ArrayDisequality();
            Console.WriteLine("\r\nArrayDisequality:");
            Print(ad.Search(Kanren.EmptyState));

            Console.WriteLine("Please press enter...");
            Console.ReadLine();
        }

        static void Print(IEnumerable<State> results, int depth = 0)
        {
            //var tmp = results.ElementAt(0).GetValues().Take(10).ToList();
            foreach (var x in results)
            {
                if (x.IsComplete)
                {
                    foreach (var y in x.GetValues().Take(7))
                    {
                        Console.Write("{0} = {1}, ", y.Key, Print(y.Value));
                    }
                    Console.WriteLine();
                }
                else
                {
                    if (!x.IsComplete && depth < 10)
                        Print(x.Continue(), depth + 1);
                }
            }
        }

        static object Print(object x)
        {
            var ie = x as System.Collections.IEnumerable;
            if (ie != null)
            {
                var sb = new StringBuilder("[");
                foreach (var y in ie) sb.AppendFormat("{0}, ", y);
                return sb.RemoveLast(2).Append("]").ToString();
            }
            return x;
        }

        public static Goal Simple()
        {
            return Kanren.Exists(x => x == 5 | x == 6);
        }

        public static Goal Simple2()
        {
            return Kanren.Exists(x => x == 5 & Kanren.Exists(y => x == y));
        }

        public static Goal SimpleConj()
        {
            return Kanren.Exists(x => x == 5)
                 & Kanren.Exists(y => y == 5 | y == 6);
        }

        static Goal OneAndNine(Kanren x)
        {
            return x == 1 & x == 9;
        }

        static Goal Fives(Kanren x)
        {
            return x == 5 | Kanren.Recurse(Fives, x);
        }

        static Goal Sixes(Kanren x)
        {
            return 6 == x | Kanren.Recurse(Sixes, x);
        }

        static Goal FivesAndSixes()
        {
            return Kanren.Exists(x => Fives(x) | Sixes(x));
        }

        static Goal FivesXorSixes()
        {
            return Kanren.Exists(z => Fives(z) & Sixes(z));
        }

        static Goal Array()
        {
            return Kanren.Exists(z => z == new[] { 1, 2, 9 });
        }

        static Goal NestedArray()
        {
            return Kanren.Exists(x => x == 99 & Kanren.Exists(z => z == new object[] { x, 2, 9 }));
        }

        static Goal ArrayEquality()
        {
            return Kanren.Exists(x => x == new[] { 1, 2 } & Kanren.Exists(z => z == x & z == new[] { 1, 2 }));
        }

        static Goal ArrayDisequality()
        {
            return Kanren.Exists(x => x == new[] { 1, 2, 3 } & Kanren.Exists(z => z == x & z == new[] { 1, 2 }));
        }
    }
}
