using System;

namespace Naggum.Lisp
{
	public class Cons : TypedAtom<Tuple<Atom, Atom>>
	{
		public static readonly Cons Nil = new Cons(null, null);

		public Cons(Atom car, Atom cdr): base(Tuple.Create(car, cdr))
		{
		}

		public Atom Car
		{
			get { return TypedValue.Item1; }
		}

		public Atom Cdr
		{
			get { return TypedValue.Item2; }
		}
	}
}
