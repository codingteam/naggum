using System.Collections.Generic;

namespace Naggum.Lisp
{
	public class TypedAtom<T> : Atom
	{
		public TypedAtom(T value)
		{
			TypedValue = value;
		}

		public override object Value
		{
			get
			{
				return TypedValue;
			}
		}

		public T TypedValue { get; private set; }

		protected bool Equals(TypedAtom<T> other)
		{
			return EqualityComparer<T>.Default.Equals(TypedValue, other.TypedValue);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((TypedAtom<T>)obj);
		}

		public override int GetHashCode()
		{
			return EqualityComparer<T>.Default.GetHashCode(TypedValue);
		}
	}
}
