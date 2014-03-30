using System;
using System.Collections.Generic;
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
			var atoms = new Stack<Atom>();
			foreach (var lexem in _lexer.Read())
			{
				switch (lexem.Kind)
				{
					case LexemKind.TokenLexem:
					{
						var tokenLexem = (TokenLexem)lexem;
						switch (tokenLexem.TokenKind)
						{
							case LexemTokenKind.OpenBrace:
								atoms.Push(Cons.Nil);
								break;

							case LexemTokenKind.CloseBrace:
								if (atoms.Count == 0)
								{
									throw new Exception("Unopened brace detected");
								}

								yield return atoms.Pop();
								break;

							case LexemTokenKind.Eof:
								if (atoms.Count != 0)
								{
									throw new Exception("Unclosed brace detected");
								}

								yield break;

							default:
								throw new NotSupportedException(
									"Lexem token kind not supported: " + tokenLexem.TokenKind);
						}

						break;
					}
					default:
						throw new NotSupportedException("Lexem kind not supported: " + lexem.Kind);
				}
			}
		}
	}
}
