using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Naggum.Lisp;

namespace Naggum.Tests
{
	[TestClass]
	public class ParserTests
	{
		[TestMethod]
		public void UnitTest()
		{
			Test("()", Cons.Nil);
		}

		private static void Test(string source, params Atom[] expected)
		{
			using (var reader = new StringReader(source))
			{
				var lexer = new Lexer(reader);
				var parser = new Parser(lexer);
				var result = parser.Parse().ToList();

				Assert.IsTrue(expected.SequenceEqual(result));
			}
		}
	}
}
