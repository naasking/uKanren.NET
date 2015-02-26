using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sasa;
using Sasa.Collections;

namespace uKanren
{
    /// <summary>
    /// A set of bindings.
    /// </summary>
    public struct State
    {
        internal Trie<Kanren, object> substitutions;
        internal int next;
        internal Func<Lifo<State>> incomplete;

        /// <summary>
        /// Get the pairs of bound variables and their values.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown when the state is incomplete.</exception>
        public IEnumerable<KeyValuePair<Kanren, object>> GetValues()
        {
            //if (!IsComplete) throw new InvalidOperationException("State is not complete.");
            // resolve any unbound variables on the fly; could make this more efficient by
            // updating the trie in-place, which is a safe operation in this case
            var subs = substitutions;
            return subs.Select(x => Tuples.Keyed(x.Key, Resolve(subs, x.Value)));
        }

        /// <summary>
        /// True if this state is final, such that all bindings have values, false if some computation remains to be done.
        /// </summary>
        internal bool IsComplete
        {
            get { return incomplete == null; }
        }

        /// <summary>
        /// Extend the set of bindings.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        internal State Extend(Kanren x, object v)
        {
            //FIXME: shouldn't duplicate a binding, but if it would, should return null?
            return new State
            {
                substitutions = substitutions.Add(x, v),
                next = next,
                incomplete = incomplete
            };
        }

        /// <summary>
        /// Recursively resolve any inner variables.
        /// </summary>
        static object Resolve(Trie<Kanren, object> substitutions, object v)
        {
            var iv = v as System.Collections.IEnumerable;
            if (iv != null && ContainsVar(iv))
            {
                var tmp = new List<object>();
                foreach (var x in iv)
                {
                    var k = x as Kanren;
                    object y;
                    tmp.Add(Resolve(substitutions, !ReferenceEquals(k, null) && substitutions.TryGetValue(k, out y) ? y : x));
                }
                return tmp;
            }
            return v;
        }

        static bool ContainsVar(System.Collections.IEnumerable iv)
        {
            foreach (var x in iv)
            {
                if (x is Kanren) return true;
                var iiv = x as System.Collections.IEnumerable;
                if (iiv != null && ContainsVar(iiv)) return true;
            }
            return false;
        }

        /// <summary>
        /// Obtain the value bound to the variable, if any.
        /// </summary>
        /// <param name="x"></param>
        /// <returns></returns>
        internal object Get(Kanren x)
        {
            object v;
            return substitutions.TryGetValue(x, out v) ? v : null;
        }

        /// <summary>
        /// Generate a new state with an updated variable index.
        /// </summary>
        /// <returns></returns>
        internal State Next(int i)
        {
            return new State { substitutions = substitutions, next = next + i, incomplete = incomplete };
        }
    }
}
