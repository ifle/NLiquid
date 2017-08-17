using System.Diagnostics;
using Nitra;
using NLiquid.Parser;
using NUnit.Framework;

namespace NLiquid.Tests
{
	[TestFixture]
	public class Class1
    {
		[Test]
		public void  WillbeRemoved()
	    {
		    var parseResult = Grammar.Start.Parse(new SourceSnapshot("{{hello}}"));
			Debug.WriteLine(parseResult);
	    }
    }
}
