# uKanren.NET
A native implementation of the MicroKanren DSL for .NET.

# Examples

Variable x may equal either 5 or 6:

    public static Goal Simple()
    {
        return Kanren.Exists(x => x == 5 | x == 6);
    }
	...
	
    var y = Simple().Search(Kanren.EmptyState);
    Print(y);

	//output:
	//x[0] = 5,
	//x[0] = 6,

Variable x may equal 5, and y may equal 5 or 6:

    public static Goal SimpleConjunction()
    {
        return Kanren.Exists(x => x == 5)
             & Kanren.Exists(y => y == 5 | y == 6);
    }
	...
    var y = SimpleConjunction().Search(Kanren.EmptyState);
    Print(y);

	//output:
	//x[0] = 5, y[0] = 6,
	//x[0] = 5, y[0] = 5,

Variable x may equal 1 and 9:

    static Goal OneAndNine(Kanren x)
    {
        return x == 1 & x == 9;
    }
	...
    var y = OneAndNine().Search(Kanren.EmptyState);
    Print(y);

	//output:

Recursive equation where variable may equal 5:

    static Goal Fives(Kanren x)
    {
        return x == 5 | Kanren.Recurse(Fives, x);
    }
	...
    var y = Fives().Search(Kanren.EmptyState);
    Print(y);

	//output:
	//x[0] = 5,
	//x[0] = 5, x[0] = 5, x[0] = 5, [stream continues]

# LICENSE

LGPL v2.1:
https://www.gnu.org/licenses/lgpl-2.1.html