using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uKanren
{
    /// <summary>
    /// A computation used to solve an equation.
    /// </summary>
    public struct Goal
    {
        internal Func<State, IEnumerable<State>> Thunk { get; set; }

        public IEnumerable<State> Search(State state)
        {
            return Thunk == null ? Enumerable.Empty<State>() : Thunk(state);
        }

        public static Goal operator |(Goal left, Goal right)
        {
            return Kanren.Disjunction(left, right);
        }

        public static Goal operator &(Goal left, Goal right)
        {
            return Kanren.Conjunction(left, right);
        }
    }
}
