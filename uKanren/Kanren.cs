using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sasa;
using Sasa.Linq;
using Sasa.Collections;

namespace uKanren
{
    public sealed class Kanren
    {
        internal int id;
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

        public struct Goal
        {
            internal Func<State, IEnumerable<State>> Thunk { get; set; }

            public IEnumerable<State> Search(State state)
            {
                return Thunk == null ? Enumerable.Empty<State>() : Thunk(state);
            }

            public static Goal operator |(Goal left, Goal right)
            {
                return Disjunction(left, right);
            }

            public static Goal operator &(Goal left, Goal right)
            {
                return Conjunction(left, right);
            }
        }

        public static State EmptyState = new State();

        public sealed class State
        {
            internal Trie<Kanren, object> substitutions;
            internal int next = 0;
            internal Goal? immature;

            public object Get(Kanren x)
            {
                object v;
                return substitutions.TryGetValue(x, out v) ? v : null;
            }

            public State Extend(Kanren x, object v)
            {
                //FIXME: shouldn't duplicate a binding, but if it would, should return null?
                return new State { substitutions = substitutions.Add(x, v), next = next };
            }

            public IEnumerable<KeyValuePair<Kanren, object>> GetValues()
            {
                //return immature == null ? substitutions : substitutions.Concat(immature.Value.Search(this).SelectMany(x => x.GetValues()));
                return substitutions;
            }

            public State Next()
            {
                return new State { substitutions = substitutions, next = next + 1 };
            }
        }

        public static Goal Exists(Func<Kanren, Goal> body)
        {
            return new Goal
            {
                Thunk = state =>
                {
                    var fn = body(new Kanren { id = state.next, Name = body.Method.GetParameters()[0].Name });
                    return fn.Search(state.Next());
                }
            };
        }

        public static Goal Conjunction(Goal left, Goal right)
        {
            return new Goal { Thunk = state => left.Search(state).SelectMany(x => right.Search(x)) };
        }

        public static Goal Disjunction(Goal left, Goal right)
        {
            return new Goal { Thunk = state => left.Search(state).Concat(right.Search(state)) };
        }

        public static Goal Recurse(Func<Kanren, Goal> body, Kanren x)
        {
            return new Goal
            {
                Thunk = state => new[] { new State { substitutions = state.substitutions, next = state.next, immature = body(x) } }
            };
        }

        public new static Goal Equals(object left, object right)
        {
            return new Goal
            {
                Thunk = state =>
                {
                    var s = Unify(left, right, state);
                    //FIXME: shouldn't this be just s? or new State { substitutions = s.substitutions, next = state.next }
                    return s != null ? new[] { s } : Enumerable.Empty<State>();
                }
            };
        }

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