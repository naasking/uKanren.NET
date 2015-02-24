using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        internal Func<IEnumerable<State>> immature;

        /// <summary>
        /// Extend the set of bindings.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        internal State Extend(Kanren x, object v)
        {
            //FIXME: shouldn't duplicate a binding, but if it would, should return null?
            return new State { substitutions = substitutions.Add(x, v), next = next, immature = immature };
        }

        public IEnumerable<KeyValuePair<Kanren, object>> GetValues()
        {
            //return immature == null ? substitutions : Enumerable.Empty<KeyValuePair<Kanren, object>>();
            //return immature == null ? substitutions : substitutions.Concat(immature().SelectMany(x => x.Values));
            // if immature != null, then this state is not yet fully resolved
            return GetBounded(1);
            //return substitutions;
        }

        IEnumerable<KeyValuePair<Kanren, object>> GetBounded(int current)
        {
            return immature == null ? substitutions:
                   current < 1000  ? immature().SelectMany(x => x.GetBounded(current + 1)):
                                      Enumerable.Empty<KeyValuePair<Kanren, object>>();
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
        internal State Next()
        {
            return new State { substitutions = substitutions, next = next + 1, immature = immature };
        }
    }
}
