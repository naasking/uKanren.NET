﻿using System;
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
        /// <summary>
        /// The globally unique variable identifier.
        /// </summary>
        public int Id { get; internal set; }

        /// <summary>
        /// The variable's name, bound to the parameter of the outer-most expression using Kanren.Exists.
        /// </summary>
        public string Name { get; internal set; }

        public override int GetHashCode()
        {
            return Id;
        }

        public override bool Equals(object other)
        {
            Kanren x = other as Kanren;
            return !ReferenceEquals(x, null) && Id == x.Id;
        }

        public override string ToString()
        {
            return Name + "[" + Id + "]";
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
                    var fn = body(new Kanren { Id = state.next, Name = arg });
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
            var args = body.Method.GetParameters();
            var arg0 = args[0].Name;
            var arg1 = args[1].Name;
            return new Goal
            {
                Thunk = state =>
                {
                    var fn = body(new Kanren { Id = state.next, Name = arg0 }, new Kanren { Id = state.next + 1, Name = arg1 });
                    return fn.Thunk(state.Next(2));
                }
            };
        }

        /// <summary>
        /// Declare an goal with two new logic variables.
        /// </summary>
        /// <param name="body">The function describing the goal given the new Kanren variable.</param>
        /// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        public static Goal Exists(Func<Kanren, Kanren, Kanren, Goal> body)
        {
            var args = body.Method.GetParameters();
            var arg0 = args[0].Name;
            var arg1 = args[1].Name;
            var arg2 = args[2].Name;
            return new Goal
            {
                Thunk = state =>
                {
                    var fn = body(
                        new Kanren { Id = state.next, Name = arg0 },
                        new Kanren { Id = state.next + 1, Name = arg1 },
                        new Kanren { Id = state.next + 2, Name = arg2 });
                    return fn.Thunk(state.Next(3));
                }
            };
        }

        /// <summary>
        /// Declare an goal with two new logic variables.
        /// </summary>
        /// <param name="body">The function describing the goal given the new Kanren variable.</param>
        /// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        public static Goal Exists(Func<Kanren, Kanren, Kanren, Kanren, Goal> body)
        {
            var args = body.Method.GetParameters();
            var arg0 = args[0].Name;
            var arg1 = args[1].Name;
            var arg2 = args[2].Name;
            var arg3 = args[3].Name;
            return new Goal
            {
                Thunk = state =>
                {
                    var fn = body(
                        new Kanren { Id = state.next, Name = arg0 },
                        new Kanren { Id = state.next + 1, Name = arg1 },
                        new Kanren { Id = state.next + 2, Name = arg2 },
                        new Kanren { Id = state.next + 3, Name = arg3 });
                    return fn.Thunk(state.Next(4));
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
            return new Goal { Thunk = state => Bind(left.Thunk(state), x => right.Thunk(x)) };
        }

        static Lifo<State> MPlus(Lifo<State> l1, Lifo<State> l2)
        {
            return l1.IsEmpty          ? l2:
                   l1.Value.IsComplete ? MPlus(l1.Next, l2) & l1.Value:
                                         new Lifo<State>(new State { incomplete = () => MPlus(l2, l1.Value.incomplete()) });
        }

        static Lifo<State> Bind(Lifo<State> l1, Func<State, Lifo<State>> selector)
        {
            return l1.IsEmpty          ? l1:
                   l1.Value.IsComplete ? MPlus(selector(l1.Value), Bind(l1.Next, selector)):
                                         new Lifo<State>(new State { incomplete = () => Bind(l1.Value.incomplete(), selector) } );
        }

        ///// <summary>
        ///// Satisfy both two goals simultaneously.
        ///// </summary>
        ///// <param name="left">The left goal to satisfy.</param>
        ///// <param name="right">The right goal to satisfy.</param>
        ///// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        //public static Goal Conjunction(params Goal[] goals)
        //{
        //    if (goals == null || goals.Length == 0) throw new ArgumentException("Conjunction needs at least one goal.");
        //    return new Goal { Thunk = state => goals.First().Thu.SelectMany(z => z.Thunk(state).SelectMany(x => right.Thunk(x))) };
        //}

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
            return new Goal { Thunk = state => MPlus(left.Thunk(state), right.Thunk(state)) };
        }

        ///// <summary>
        ///// Satisfy either of the two goals.
        ///// </summary>
        ///// <param name="left">The left goal to satisfy.</param>
        ///// <param name="right">The right goal to satisfy.</param>
        ///// <returns>A <see cref="Goal"/> describing the equalities to satisfy.</returns>
        //public static Goal Disjunction(params Goal[] goals)
        //{
        //    // concat works here, but is less fair than interleaving evaluation between left and right
        //    //return new Goal { Thunk = state => left.Thunk(state).Concat(right.Thunk(state)) };
        //    return new Goal { Thunk = state => Interleave(left.Thunk(state), right.Thunk(state)) };
        //}

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
                Thunk = state => new Lifo<State>(new State { incomplete = () => body(x).Thunk(state) })
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
                    return s != null ? new Lifo<State>(s.Value) : Lifo<State>.Empty;
                }
            };
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

        static State? Unify(object uorig, object vorig, State s)
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
                return UnifyEnumerable(iu, iv, s);
            if (u.Equals(v))
                return s;
            return null;
        }

        static State? UnifyEnumerable(System.Collections.IEnumerable iu, System.Collections.IEnumerable iv, State? s)
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
                s = Unify(eu.Current, ev.Current, s.Value);
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