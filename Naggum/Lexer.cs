using System.Collections.Generic;
using System.IO;
using Naggum.Lexems;

namespace Naggum
{
	public class Lexer
	{
		private readonly TextReader _reader;

		public Lexer(TextReader reader)
		{
			_reader = reader;
		}

		public IEnumerable<Lexem> Read()
		{
			int charactedRead;
			while ((charactedRead =  _reader.Read()) != -1)
			{
				var character = (char)charactedRead;
				switch (character)
				{
					case '(':
						yield return LexemFactory.CreateLexem(LexemTokenKind.OpenBrace);
						continue;
					case ')':
						yield return LexemFactory.CreateLexem(LexemTokenKind.CloseBrace);
						continue;
					case '\'':
						yield return LexemFactory.CreateLexem(LexemTokenKind.Quote);
						continue;
				}

				if (char.IsWhiteSpace(character))
				{
					continue;
				}

				// TODO: Parse identifiers.
			}

			yield return LexemFactory.CreateLexem(LexemTokenKind.Eof);
		}
	}
}
