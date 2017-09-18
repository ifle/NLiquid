using System;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using Nitra;
using Nitra.Declarations;
using NLiquid.Parser.Ast;
using NUnit.Framework;

namespace NLiquid.Tests
{
	[TestFixture]
	public class EmitTemplateMethodTests
	{
		private static Func<Expression,string> _getDebugView;

		/// <summary>
		/// Gets the DebugView internal property value of provided expression.
		/// </summary>
		/// <param name="expression">Expression to get DebugView.</param>
		/// <returns>DebugView value.</returns>
		public static string GetDebugView(Expression expression)
		{
			if (_getDebugView == null)
			{
				var p = Expression.Parameter(typeof(Expression));

				var l = Expression.Lambda<Func<Expression,string>>(
					Expression.PropertyOrField(p, "DebugView"),
					p);

				_getDebugView = l.Compile();
			}

			return _getDebugView(expression);
		}

		[Test]
		public void CreateVariable_Test()
		{
			//var parameter = new Parameter(Location.NoLocation, new Name(Location.NoLocation, "product"), new QualifiedReference.Simple(Location.NoLocation, new Reference(Location.NoLocation)));

			//parameter.TypeRef.Symbol
			//Expression.Variable
			//var func = Expression.Lambda(
			//	Expression.Block(
			//		Expression.Call(typeof(PredefinedTypesTest).GetMethod("Log", BindingFlags.Public | BindingFlags.Static), Expression.Constant("Igor", typeof(string)))
			//	)
			//);
			TextWriter writer;
			//writer.WriteLine();
			//writer.WriteLine();
			var parameters = new[] { Expression.Parameter(typeof(TextWriter), "writer")};
			var methodInfo2 = typeof(Console).GetMethod("WriteLine", new [] { typeof(object) });
			var methodInfo = typeof(TextWriter).GetMethod("WriteLine", new [] { typeof(object) } );
			var func = Expression.Lambda(
				Expression.Block(
					new[] { Expression.Variable(typeof(string), "igorvar") },
					Expression.Call(parameters[0], methodInfo)
					),
				parameters
			);
			Console.WriteLine(GetDebugView(func));
			//var del = func.Compile();
			//del.DynamicInvoke();
		}

		public static void Log(string s)
		{
			Console.WriteLine(s);
		}
	}
}