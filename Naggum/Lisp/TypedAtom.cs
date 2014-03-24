using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Naggum.Lisp
{
	public class TypedAtom<T> : Atom
	{
		public T TypedValue
		{
			get { return (T)Value; }
			set { Value = value; }
		}
	}
}
