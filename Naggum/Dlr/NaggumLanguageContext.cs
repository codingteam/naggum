using System.Collections.Generic;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;

namespace Naggum.Dlr
{
	public class NaggumLanguageContext : LanguageContext
	{
		public NaggumLanguageContext(ScriptDomainManager domainManager, IDictionary<string, object> options)
			: base(domainManager)
		{
		}

		public override ScriptCode CompileSourceCode(
			SourceUnit sourceUnit,
			CompilerOptions options,
			ErrorSink errorSink)
		{
			return new NaggumScriptCode(sourceUnit);
		}
	}
}
