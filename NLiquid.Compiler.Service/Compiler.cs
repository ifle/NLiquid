using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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

            foreach (var compileUnit in asts)
            {
                if (compileUnit.Field1)
                {
                    var ast = compileUnit.Field0;
                    var file = ast.Location.Source.File;
                    var templateName = Path.GetFileNameWithoutExtension(file.Name).Replace(".", "_").Replace("-", "_");
                    MethodBuilder methodBuilder = tb.DefineMethod(templateName, MethodAttributes.Public | MethodAttributes.Static, null, null);
                    Expression<Action> expressionTree = EmitTemplateMethod(ast, methodBuilder);
                    expressionTree.CompileToMethod(methodBuilder);
                }
            }

			tb.CreateType();
			ab.Save(dllName);
		}

        Expression<Action> EmitTemplateMethod(IAst field0, MethodBuilder methodBuilder)
        {
            // генерируем код
            return () => Console.WriteLine("Здесь надо сгенерировать код");
        }
    }
}
