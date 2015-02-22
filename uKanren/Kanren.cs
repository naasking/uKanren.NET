using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sasa;
using Sasa.Collections;

namespace uKanren
{
    public abstract class Kanren
    {
        public abstract bool Equals(Kanren other);

        static Kanren Walk(Kanren uvar, State env)
        {
            // search for final Var binding in env
            Var v;
            while (uvar != null)
            {
                v = uvar as Var;
                if (v == null) break;
                uvar = env.Get(v);
            }
            return uvar;
        }

        static State Unify(Kanren uorig, Kanren vorig, State s)
        {
            var u = Walk(uorig, s) ?? uorig;
            var v = Walk(vorig, s) ?? vorig;
            var uvar = u as Var;
            var vvar = v as Var;
            var equ = u as Pair;
            var eqv = v as Pair;
            return uvar != null && vvar != null && vvar.Equals(uvar) ? s:
                   uvar != null                                      ? s.Extend(uvar, v):
                   vvar != null                                      ? s.Extend(vvar, u):
                   equ != null && eqv != null                        ? Unify(equ, eqv, s):
                   u.Equals(v)                                       ? Unify(u, v, s):
                                                                       null;
        }

        static State Unify(Pair u, Pair v, State s)
        {
            s = Unify(u.left, v.left, s);
            return s == null ? s : Unify(u.right, v.right, s);
        }

        public sealed class Goal
        {
            public Func<State, IEnumerable<State>> Value { get; set; }

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
            public Vector<KeyValuePair<Var, Kanren>?> substitutions;
            public int next = 1;

            public Kanren Get(Var x)
            {
                var z = substitutions[x.id];
                return z != null ? z.Value.Value : null;
            }

            public State Extend(Var x, Kanren v)
            {
                return new State { substitutions = substitutions.Set(x.id, Tuples.Keyed(x, v)), next = next };
            }

            public IEnumerable<KeyValuePair<Var, Kanren>> GetValues()
            {
                for (var i = 0; i < substitutions.Count; ++i)
                {
                    var x = substitutions[i];
                    if (x != null) yield return x.Value;
                }
            }

            public State Next()
            {
                // ensure substitutions is as large as is needed to accomodate all possible variables
                return new State { substitutions = substitutions.Add(null), next = next + 1 };
            }
        }

        public static Goal Exists<T>(Func<Var<T>, Goal> body)
        {
            return new Goal
            {
                Value = state =>
                {
                    var fn = body(new Var<T> { id = state.next, Name = body.Method.GetParameters()[0].Name }).Value;
                    return fn(state.Next());
                }
            };
        }

        public static Goal Conjunction(Goal left, Goal right)
        {
            return new Goal { Value = state => left.Value(state).SelectMany(x => right.Value(x)) };
        }

        public static Goal Disjunction(Goal left, Goal right)
        {
            return new Goal { Value = state => left.Value(state).Concat(right.Value(state)) };
        }

        public static Goal Equal(Kanren left, Kanren right)
        {
            return new Goal
            {
                Value = state =>
                {
                    var s = Unify(left, right, state);
                    //FIXME: not right
                    return s != null ? new[] { new State { substitutions = s.substitutions, next = s.next } }:
                                       Enumerable.Empty<State>();
                }
            };
        }

        public static Goal Simple()
        {
            return Exists<int>(x => x == 5);
        }

        public static Goal SimpleConj()
        {
            return Exists<int>(x => x == 5)
                 & Exists<int>(y => y == 5 | y == 6);
        }

        #region Kanren terms
        public abstract class Var : Kanren
        {
            internal int id;
            public string Name { get; internal set; }
            public override string ToString()
            {
                return Name + "[" + id + "]";
            }
        }
        public sealed class Var<T> : Var
        {
            public override bool Equals(Kanren other)
            {
                var x = other as Var<T>;
                return x != null && id == x.id;
            }

            public static Goal operator ==(Var<T> left, T right)
            {
                return Equal(left, new Val<T> { value = right });
            }

            public static Goal operator ==(T left, Var<T> right)
            {
                return Equal(new Val<T> { value = left }, right);
            }

            #region Inequalities
            public static Goal operator !=(Var<T> left, T right)
            {
                throw new NotSupportedException();
            }

            public static Goal operator !=(T left, Var<T> right)
            {
                throw new NotSupportedException();
            }
            #endregion
        }

        public sealed class Val<T> : Kanren
        {
            public T value;

            public override bool Equals(Kanren other)
            {
                var x = other as Val<T>;
                return x != null && EqualityComparer<T>.Default.Equals(value, x.value);
            }
            public override string ToString()
            {
                return value.ToString();
            }
        }

        public sealed class Pair : Kanren
        {
            public Kanren left;
            public Kanren right;

            public override bool Equals(Kanren other)
            {
                var x = other as Pair;
                return x != null && left.Equals(x.left) && right.Equals(x.right);
            }
        }
        #endregion
    }
}