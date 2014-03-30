using System;
using System.Collections.Generic;
using System.Linq;

namespace Naggum.Lisp
{
	public class Cons : TypedAtom<Tuple<Atom, Atom>>
	{
		public static readonly Cons Nil = new Cons(null, null);

		public static Cons FromEnumerable(IEnumerable<Atom> enumerable)
		{
			Cons result = Nil;
			foreach (var atom in enumerable.Reverse())
			{
				result = new Cons(atom, result);
			}

			return result;
		}

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
