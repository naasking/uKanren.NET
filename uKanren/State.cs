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
    public sealed class State
    {
        internal Trie<Kanren, object> substitutions;
        internal int next = 0;
        internal Func<Lifo<State>> incomplete;

        /// <summary>
        /// True if this state is final, such that all bindings have values, false if some computation remains to be done.
        /// </summary>
        public bool IsComplete
        {
            get { return incomplete == null; }
        }

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
            return substitutions.Select(x => Tuples.Keyed(x.Key, Resolve(x.Value)));
        }

        /// <summary>
        /// Continue any remaining computation and return the stream of states it generates, if any.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">Thrown if state is complete.</exception>
        public IEnumerable<State> Continue()
        {
            if (IsComplete) throw new InvalidOperationException("State is complete.");
            //return incomplete().Thunk(this);
            return incomplete();
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
        object Resolve(object v)
        {
            var iv = v as System.Collections.IEnumerable;
            if (iv != null && ContainsVar(iv))
            {
                var s = this;
                var tmp = new List<object>();
                foreach (var x in iv)
                {
                    var k = x as Kanren;
                    object y;
                    tmp.Add(Resolve(!ReferenceEquals(k, null) && substitutions.TryGetValue(k, out y) ? y : x));
                }
                return tmp;
            }
            return v;
        }

        bool ContainsVar(System.Collections.IEnumerable iv)
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
