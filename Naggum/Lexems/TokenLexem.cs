﻿namespace Naggum.Lexems
{
	public class TokenLexem : Lexem
	{
		public LexemTokenKind TokenKind { get; private set; }

		public TokenLexem(LexemTokenKind tokenKind)
		{
			TokenKind = tokenKind;
		}

		protected bool Equals(TokenLexem other)
		{
			return TokenKind == other.TokenKind;
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != this.GetType()) return false;
			return Equals((TokenLexem)obj);
		}

		public override int GetHashCode()
		{
			return (int)TokenKind;
		}
	}
}
