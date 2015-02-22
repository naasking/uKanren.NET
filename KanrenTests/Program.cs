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
            var x = Simple().Value(Kanren.EmptyState);
            Console.WriteLine("\r\nSimple:");
            Print(x);

            var y = SimpleConj().Value(Kanren.EmptyState);
            Console.WriteLine("\r\nSimpleConj:");
            Print(y);

            var fv = Kanren.Exists<int>(Fives).Value;
            Console.WriteLine("\r\nFives:");
            Print(fv(Kanren.EmptyState));

            Console.WriteLine("Please press enter...");
            Console.ReadLine();
        }

        static void Print(IEnumerable<Kanren.State> results)
        {
            foreach (var x in results)
            {
                foreach (var y in x.GetValues())
                {
                    Console.Write("{0} = {1}, ", y.Key, y.Value);
                }
                Console.WriteLine();
            }
        }

        public static Kanren.Goal Simple()
        {
            return Kanren.Exists<int>(x => x == 5);
        }

        public static Kanren.Goal SimpleConj()
        {
            return Kanren.Exists<int>(x => x == 5)
                 & Kanren.Exists<int>(y => y == 5 | y == 6);
        }

        static Kanren.Goal Fives(Kanren.Var<int> x)
        {
            return x == 5 | Kanren.Recursive(Fives, x);
        }
    }
}
