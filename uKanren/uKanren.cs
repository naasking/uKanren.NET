using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace uKanren.Core
{
    public class MicroKanren
    {
        public delegate IEnumerable<State> Goal(State state);

        public static Goal Conjunction(Goal left, Goal right)
        {
            return state => left(state).SelectMany(x => right(x));
        }

        public static Goal Disjunction(Goal left, Goal right)
        {
            return state => left(state).Concat(right(state));
        }

        public static Goal Recurse(Func<Kanren, Goal> body, Kanren x)
        {
            return state => new[]
            {
                new State { substitutions = state.substitutions, next = state.next, immature = body(x) }
            };
        }

        public new static Goal Equals(object left, object right)
        {
            return state =>
            {
                var s = Unify(left, right, state);
                //FIXME: shouldn't this be just s? or new State { substitutions = s.substitutions, next = state.next }
                return s != null ? new[] { s } : Enumerable.Empty<State>();
            };
        }
    }
}
