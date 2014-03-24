using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Naggum.Lisp
{
	public class Cons : TypedAtom<Tuple<Atom, Atom>>
	{
		public Atom Car {
			get { return TypedValue.Item1; }
			set { TypedValue = Tuple.Create(value, TypedValue.Item2); } 
		}

		public Atom Cdr
		{
			get { return TypedValue.Item2; }
			set { TypedValue = Tuple.Create(TypedValue.Item1, value); }
		}
	}
}
