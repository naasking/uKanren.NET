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

            var fs = FivesAndSixes();
            Console.WriteLine("\r\nFives and Sixes:");
            Print(fs.Search(Kanren.EmptyState));

            var fx = FivesXorSixes();
            Console.WriteLine("\r\nFives xor Sixes:");
            Print(fx.Search(Kanren.EmptyState));

            Console.WriteLine("Please press enter...");
            Console.ReadLine();
        }

        static void Print(IEnumerable<State> results)
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
            //return Kanren.Exists(x => x == 5 & x == 6);
        }
    }
}
