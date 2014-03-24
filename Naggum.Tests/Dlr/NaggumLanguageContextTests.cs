using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Naggum.Dlr;

namespace Naggum.Tests.Dlr
{
	[TestClass]
	public class NaggumLanguageContextTests
	{
		private ScriptEngine _engine;

		[TestInitialize]
		public void Initialize()
		{
			var setup = new ScriptRuntimeSetup();

			var typeName = typeof (NaggumLanguageContext).AssemblyQualifiedName;
			setup.LanguageSetups.Add(
				new LanguageSetup(typeName, "Naggum", new[] { "naggum" }, new[] { ".naggum" }));
			var runtime = new ScriptRuntime(setup);
			_engine = runtime.GetEngine("naggum");
		}

		[TestMethod]
		public void RunTest()
		{
			Test("\"run result\"", "run result");
		}

		private void Test(string code, object expectedResult)
		{
			var result = _engine.Execute(code);
			Assert.AreEqual(expectedResult, result);
		}
	}
}
