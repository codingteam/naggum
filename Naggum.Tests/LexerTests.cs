using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Naggum.Lexems;

namespace Naggum.Tests
{
	[TestClass]
    public class LexerTests
    {
		[TestMethod]
		public void EmptyInputTest()
		{
			Test(string.Empty, LexemFactory.CreateLexem(LexemTokenKind.Eof));
		}

		[TestMethod]
		public void UnitTest()
		{
			Test(
				"'()",
				LexemFactory.CreateLexem(LexemTokenKind.Quote),
				LexemFactory.CreateLexem(LexemTokenKind.OpenBrace),
				LexemFactory.CreateLexem(LexemTokenKind.CloseBrace),
				LexemFactory.CreateLexem(LexemTokenKind.Eof));
		}

		private void Test(string source, params Lexem[] expected)
		{
			using (var reader = new StringReader(source))
			{
				var lexer = new Lexer(reader);
				var result = lexer.Read().ToList();

				Assert.IsTrue(expected.SequenceEqual(result));
			}
		}
    }
}
