namespace Naggum.Lexems
{
	public static class LexemFactory
	{
		public static TokenLexem Quote = CreateLexem(LexemTokenKind.Quote);
		public static TokenLexem OpenBrace = CreateLexem(LexemTokenKind.OpenBrace);
		public static TokenLexem CloseBrace = CreateLexem(LexemTokenKind.CloseBrace);
		public static TokenLexem Eof = CreateLexem(LexemTokenKind.Eof);

		public static TokenLexem CreateLexem(LexemTokenKind tokenKind)
		{
			return new TokenLexem(tokenKind);
		}
	}
}
