using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Scripting;
using Microsoft.Scripting.Runtime;

namespace Naggum.Dlr
{
	internal class NaggumScriptCode : ScriptCode
	{
		public NaggumScriptCode(SourceUnit sourceUnit) : base(sourceUnit)
		{
		}

		public override object Run(Scope scope)
		{
			return "run result";
		}
	}
}
