# uKanren.NET

A native implementation of the MicroKanren logic programming DSL for .NET. The paper describing the
original Scheme implementation is here:

http://webyrd.net/scheme-2013/papers/HemannMuKanren2013.pdf

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
    var y = Kanren.Exists(OneAndNine).Search(Kanren.EmptyState);
    Print(y);

	//output:

Recursive equation where variable may equal 5:

    static Goal Fives(Kanren x)
    {
        return x == 5 | Kanren.Recurse(Fives, x);
    }
	...
    var y = Kanren.Exists(Fives).Search(Kanren.EmptyState);
    Print(y);

	//output:
	//x[0] = 5,
	//x[0] = 5, x[0] = 5, x[0] = 5, [stream continues]

Resolve variables within arrays:

    static Goal DoublyNestedArray()
    {
        return Kanren.Exists(x => x == new[] { 3, 99 }
			 & Kanren.Exists(z => z == new object[] { x, 2, 9 }));
    }
	...
    var y = Kanren.Exists(DoublyNestedArrays).Search(Kanren.EmptyState);
    Print(y);

	//output:
	//x[0] = [3, 99], z[1] = [[3, 99], 2, 9]

# LICENSE

LGPL v2.1:
https://www.gnu.org/licenses/lgpl-2.1.html