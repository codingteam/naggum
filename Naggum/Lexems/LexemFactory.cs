namespace Naggum.Lexems
{
	public static class LexemFactory
	{
		public static TokenLexem Eof = CreateLexem(LexemTokenKind.Eof);

		public static TokenLexem CreateLexem(LexemTokenKind tokenKind)
		{
			return new TokenLexem(tokenKind);
		}
	}
}
