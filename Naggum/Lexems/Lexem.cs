namespace Naggum.Lexems
{
	public abstract class Lexem
	{
		public LexemKind Kind { get; private set; }

		protected Lexem(LexemKind kind)
		{
			Kind = kind;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((Lexem)obj);
		}

		protected bool Equals(Lexem other)
		{
			return Kind == other.Kind;
		}

		public override int GetHashCode()
		{
			return (int)Kind;
		}
	}
}
