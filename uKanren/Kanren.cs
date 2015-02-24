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
        public static Goal Exists(Func<Kanren, Goal> body)
        {
            return new Goal
            {
                Thunk = state =>
                {
                    var fn = body(new Kanren { id = state.next, Name = body.Method.GetParameters()[0].Name });
                    return fn.Thunk(state.Next());
                }
            };
        }

        public static Goal Conjunction(Goal left, Goal right)
        {
            return new Goal { Thunk = state => left.Thunk(state).SelectMany(x => right.Thunk(x)) };
        }

        public static Goal Disjunction(Goal left, Goal right)
        {
            //return new Goal { Thunk = state => left.Thunk(state).Concat(right.Thunk(state)) };
            return new Goal { Thunk = state => Interleave(left.Thunk(state), right.Thunk(state)) };
        }

        public static Goal Recurse(Func<Kanren, Goal> body, Kanren x)
        {
            // propagate the current state to the nested/immature stream, don't need to save substitutions or next variables
            return new Goal
            {
                Thunk = state => new[] { new State { immature = () => body(x).Thunk(state) } }
            };
        }

        public static Goal Recurse(Func<Goal> body)
        {
            // propagate the current state to the nested/immature stream, don't need to save substitutions or next variables
            return new Goal
            {
                Thunk = state => new[] { new State { immature = () => body().Thunk(state) } }
            };
        }

        public new static Goal Equals(object left, object right)
        {
            return new Goal
            {
                Thunk = state =>
                {
                    var s = Unify(left, right, state);
                    return s != null ? new[] { s } : Enumerable.Empty<State>();
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
            if (u.Equals(v))
                return Unify(u, v, s);
            return null;
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