using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sasa.Collections;

namespace uKanren
{
    /// <summary>
    /// A computation used to solve an equation.
    /// </summary>
    public struct Goal
    {
        internal Func<State, Lifo<State>> Thunk { get; set; }

        /// <summary>
        /// Run the search given a state.
        /// </summary>
        /// <param name="state">The starting state.</param>
        /// <returns>The set of states that satisfy the goals.</returns>
        public IEnumerable<State> Search(State state = null)
        {
            // evaluate the stream lazily, invoking and tracking incomplete states as we encounter them
            if (Thunk == null) yield break;
            var queued = new Queue<Lifo<State>>();
            for (var x = Thunk(state ?? Kanren.EmptyState); !x.IsEmpty || queued.Count > 0; x = x.Next)
            {
                while (x.IsEmpty) x = queued.Dequeue();
                if (x.Value.IsComplete)
                    yield return x.Value;
                else
                    queued.Enqueue(x.Value.incomplete());
            }
        }

        public static Goal operator |(Goal left, Goal right)
        {
            return Kanren.Disjunction(left, right);
        }

        public static Goal operator &(Goal left, Goal right)
        {
            return Kanren.Conjunction(left, right);
        }

        public override string ToString()
        {
            var x = Thunk.Method.Name.Split('<', '>');
            return x.Length >= 3 ? x[1] : Thunk.Method.Name;
        }
    }
}
