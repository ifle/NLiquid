using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nemerle.Builtins;
using Nitra.Declarations;
using Nitra.ProjectSystem;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.IO;
using NLiquid.Parser.Ast;

namespace NLiquid.Compiler.Service
{
	public class Compiler : IPostprocessing
	{
		public void Postprocessing(CancellationToken cancellationToken, Project project, ImmutableArray<Nemerle.Builtins.Tuple<IAst, bool>> asts, ProjectData data)
		{
			Console.WriteLine("Postprocessing!!!");

			AppDomain ad = AppDomain.CurrentDomain;
			AssemblyName am = new AssemblyName();
            var name = Path.GetFileName(project.ProjectDir);
            var dllName = name + ".dll";
            am.Name = name;
			AssemblyBuilder ab = ad.DefineDynamicAssembly(am, AssemblyBuilderAccess.Save);
			ModuleBuilder mb = ab.DefineDynamicModule(name + "Mod", dllName);
			TypeBuilder tb = mb.DefineType("NLiquidTemplates", TypeAttributes.Public);
			var context = data.Context;
            foreach (var compileUnit in asts)
                if (compileUnit.Field1)
                    EmitTemplateMethod(compileUnit.Field0, tb, context);

			tb.CreateType();
			ab.Save(dllName);
		}

        void EmitTemplateMethod(IAst ast, TypeBuilder tb, NLiquidDependentPropertyEvalContext context)
        {
			var file = ast.Location.Source.File;
            var templateName = Path.GetFileNameWithoutExtension(file.Name).Replace(".", "_").Replace("-", "_");

            var methodBuilder = tb.DefineMethod(templateName, MethodAttributes.Public | MethodAttributes.Static, null, null);
			// the first parameter of tempalte method is TextWriter
	        var methodParameters = new List<ParameterExpression>{  Expression.Parameter(typeof(TextWriter), "writer") };

			var commentParameter = EmitCommentParameter(ast, context);
			if(commentParameter != null)
				methodParameters.Add(commentParameter);

	        var templateMethod = Expression.Lambda(
				Expression.Block(
						//new []  { Expression.Variable(typeof(string), "igorvar") },
						Expression.Call(methodParameters[0],
							typeof(TextWriter).GetMethod("WriteLine", new [] { typeof(object) } ),
							commentParameter != null ? (Expression) commentParameter : Expression.Constant("Hello"))
					),
				methodParameters
				);
	        templateMethod.CompileToMethod(methodBuilder);
        }

		private static ParameterExpression EmitCommentParameter(IAst ast, NLiquidDependentPropertyEvalContext context)
		{
			ParameterExpression result = null;
			var parameter = ((CompilationUnit) ast).Parameter;
			if (parameter.HasValue)
			{
				var dotnetType = context.PredefinedTypes.GetDotNetType(parameter.Value.TypeRef.Symbol);
				if (dotnetType != null)
				{
					result = Expression.Parameter(dotnetType, parameter.Value.Name.Text);
				}
				else
					Debug.Assert(false, $".NET Type for {parameter.Value.TypeRef.Symbol} is null");
			}
			return result;
		}
	}
}
