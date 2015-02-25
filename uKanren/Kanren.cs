using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sasa;
using Sasa.Linq;
using Sasa.Collections;

namespace uKanren
{
    /// <summary>
    /// A Kanren logic variable.
    /// </summary>
    public sealed class Kanren
    {
        internal int id;

        /// <summary>
        /// The variable's name, bound to the parameter of the outer-most expression using Kanren.Exists.
        /// </summary>
        public string Name { get; internal set; }

        public override int GetHashCode()
        {
            return id;
        }

        public override bool Equals(object other)
        {
            Kanren x = other as Kanren;
            return !ReferenceEquals(x, null) && id == x.id;
        }

        public override string ToString()
        {
            return Name + "[" + id + "]";
        }

        public static State EmptyState = new State();

        #region Core uKanren operators
        /// <summary>
        /// Declare an goal with a new logic variable.
        /// </summary>
        /// <param name="body">The function describing the goal given the new Kanren variable.</param>
        /// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        public static Goal Exists(Func<Kanren, Goal> body)
        {
            var arg = body.Method.GetParameters()[0].Name;
            return new Goal
            {
                Thunk = state =>
                {
                    var fn = body(new Kanren { id = state.next, Name = arg });
                    return fn.Thunk(state.Next(1));
                }
            };
        }

        /// <summary>
        /// Declare an goal with two new logic variables.
        /// </summary>
        /// <param name="body">The function describing the goal given the new Kanren variable.</param>
        /// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        public static Goal Exists(Func<Kanren, Kanren, Goal> body)
        {
            var arg0 = body.Method.GetParameters()[0].Name;
            var arg1 = body.Method.GetParameters()[1].Name;
            return new Goal
            {
                Thunk = state =>
                {
                    var fn = body(new Kanren { id = state.next, Name = arg0 }, new Kanren { id = state.next + 1, Name = arg1 });
                    return fn.Thunk(state.Next(2));
                }
            };
        }

        /// <summary>
        /// Satisfy both two goals simultaneously.
        /// </summary>
        /// <param name="left">The left goal to satisfy.</param>
        /// <param name="right">The right goal to satisfy.</param>
        /// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        public static Goal Conjunction(Goal left, Goal right)
        {
            return new Goal { Thunk = state => left.Thunk(state).SelectMany(x => right.Thunk(x)) };
        }

        /// <summary>
        /// Satisfy either of the two goals.
        /// </summary>
        /// <param name="left">The left goal to satisfy.</param>
        /// <param name="right">The right goal to satisfy.</param>
        /// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        public static Goal Disjunction(Goal left, Goal right)
        {
            // concat works here, but is less fair than interleaving evaluation between left and right
            //return new Goal { Thunk = state => left.Thunk(state).Concat(right.Thunk(state)) };
            return new Goal { Thunk = state => Interleave(left.Thunk(state), right.Thunk(state)) };
        }

        /// <summary>
        /// Satisfy both two goals simultaneously.
        /// </summary>
        /// <param name="left">The left goal to satisfy.</param>
        /// <param name="right">The right goal to satisfy.</param>
        /// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        public static Goal Recurse(Func<Kanren, Goal> body, Kanren x)
        {
            return new Goal
            {
                Thunk = state => new Lifo<State>(new State { incomplete = () => body(x) })
            };
        }

        /// <summary>
        /// Ensure the left value equals the right value.
        /// </summary>
        /// <param name="left">The left value to unify.</param>
        /// <param name="right">The right value to unify.</param>
        /// <returns>A <see cref="Goal"/> that unifies <paramref name="left"/> and <paramref name="right"/> values.</returns>
        public new static Goal Equals(object left, object right)
        {
            return new Goal
            {
                Thunk = state =>
                {
                    var s = Unify(left, right, state);
                    return s != null ? new Lifo<State>(s) : Enumerable.Empty<State>();
                }
            };
        }

        static IEnumerable<State> Interleave(IEnumerable<State> left, IEnumerable<State> right)
        {
            using (var eleft = left.GetEnumerator())
            {
                using (var eright = right.GetEnumerator())
                {
                    bool bleft, bright;
                    do
                    {
                        bleft = eleft.MoveNext();
                        bright = eright.MoveNext();
                        if (bleft) yield return eleft.Current;
                        if (bright) yield return eright.Current;
                    } while(bleft || bright);
                }
            }
        }
        #endregion

        #region Unification
        static object Walk(object uvar, State env)
        {
            // search for final Kanren binding in env
            Kanren v;
            while (true)
            {
                v = uvar as Kanren;
                if (ReferenceEquals(v, null)) break;
                var tmp = env.Get(v);
                if (ReferenceEquals(tmp, null)) break;
                uvar = tmp;
            }
            return uvar;
        }

        static State Unify(object uorig, object vorig, State s)
        {
            var u = Walk(uorig, s);
            var v = Walk(vorig, s);
            var uvar = u as Kanren;
            var vvar = v as Kanren;
            if (!ReferenceEquals(uvar, null) && !ReferenceEquals(vvar, null) && uvar.Equals(vvar))
                return s;
            if (!ReferenceEquals(uvar, null))
                return s.Extend(uvar, v);
            if (!ReferenceEquals(vvar, null))
                return s.Extend(vvar, u);
            var iu = u as System.Collections.IEnumerable;
            var iv = v as System.Collections.IEnumerable;
            if (iu != null && iv != null)
                return Unify(iu, iv, s);
            if (u.Equals(v))
                return s;
            return null;
        }

        static State Unify(System.Collections.IEnumerable iu, System.Collections.IEnumerable iv, State s)
        {
            var eu = iu.GetEnumerator();
            var ev = iv.GetEnumerator();
            bool bu, bv;
            do
            {
                bu = eu.MoveNext();
                bv = ev.MoveNext();
                // sequences don't unify when either is shorter than the other
                if (bu ^ bv) return null;
                if (!bu && !bv) break;
                s = Unify(eu.Current, ev.Current, s);
            } while (s != null);
            return s;
        }
        #endregion

        #region Equalities
        public static Goal operator ==(object left, Kanren right)
        {
            return Equals(left, right);
        }

        public static Goal operator ==(Kanren left, object right)
        {
            return Equals(left, right);
        }

        public static Goal operator ==(Kanren left, Kanren right)
        {
            return Equals(left, right);
        }
        #endregion

        #region Inequalities
        public static Goal operator !=(object left, Kanren right)
        {
            throw new NotSupportedException();
        }
        public static Goal operator !=(Kanren left, object right)
        {
            throw new NotSupportedException();
        }
        public static Goal operator !=(Kanren left, Kanren right)
        {
            throw new NotSupportedException();
        }
        #endregion
    }
}