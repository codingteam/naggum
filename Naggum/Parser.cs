using System;
using System.Collections.Generic;
using System.Linq;
using Naggum.Lexems;
using Naggum.Lisp;

namespace Naggum
{
	public class Parser
	{
		private readonly Lexer _lexer;

		public Parser(Lexer lexer)
		{
			_lexer = lexer;
		}

		public IEnumerable<Atom> Parse()
		{
			using (var state = _lexer.Read().GetEnumerator())
			{
				foreach (var atom in ReadList(state, LexemFactory.Eof))
				{
					yield return atom;
				}
			}
		}

		private Atom ReadAtom(IEnumerator<Lexem> state)
		{
			var lexem = state.Current;
			switch (lexem.Kind)
			{
				case LexemKind.TokenLexem:
				{
					var tokenLexem = (TokenLexem)lexem;
					switch (tokenLexem.TokenKind)
					{
						case LexemTokenKind.OpenBrace:
							var innerAtoms = ReadList(state, LexemFactory.CloseBrace).ToList();
							if (!LexemFactory.CloseBrace.Equals(state.Current))
							{
								throw new Exception("Unexpected " + state.Current);
							}

							state.MoveNext();
							return Cons.FromEnumerable(innerAtoms);

						default:
							throw new Exception("Unexpected " + tokenLexem);
					}
				}
				default:
					throw new Exception("Unexpected " + lexem.Kind);
			}
		}

		private IEnumerable<Atom> ReadList(IEnumerator<Lexem> state, TokenLexem finalToken)
		{
			state.MoveNext();
			while (!finalToken.Equals(state.Current))
			{
				yield return ReadAtom(state);
			}
		}
	}
}
