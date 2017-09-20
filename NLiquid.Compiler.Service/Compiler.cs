using System;
using System.CodeDom;
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
		private static MethodInfo _defaultTextWriteMethodInfo = typeof(TextWriter).GetMethod("WriteLine", new[] { typeof(object) });
		private static Dictionary<System.Type, MethodInfo> _writeTextWriteMethodInfoMap = new Dictionary<System.Type, MethodInfo>
		{
			{ typeof(int),     typeof(TextWriter).GetMethod("Write", new[] { typeof(int) }) },
			{ typeof(double),  typeof(TextWriter).GetMethod("Write", new[] { typeof(double) }) },
			{ typeof(string),  typeof(TextWriter).GetMethod("Write", new[] { typeof(string) }) },
			{ typeof(bool),    typeof(TextWriter).GetMethod("Write", new[] { typeof(bool) }) },
		};

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
			var compilationUnit            = (CompilationUnit)ast;
			var methodBuilder              = CreatreMethodBuilder(ast, tb);
	        var textWriterParameter        = Expression.Parameter(typeof(TextWriter), "writer");
			var variablesMap               = new Dictionary<LocalVariableSymbol,Expression>();
			var statements                 = new List<Expression>();

			#region EmitTextWriteStatement

			void EmitTextWriteStatement<T>(T value)
			{
				var valueType = typeof(T);

				if (!_writeTextWriteMethodInfoMap.TryGetValue(valueType, out var methodInfo))
					methodInfo = _defaultTextWriteMethodInfo;
				statements.Add(Expression.Call(textWriterParameter, methodInfo, Expression.Constant(value, valueType)));
			}

	        #endregion

			#region EmitCommentParameter

			ParameterExpression EmitCommentParameter()
	        {
		        ParameterExpression result = null;
		        var parameter = compilationUnit.Parameter;
		        if (parameter.HasValue)
		        {
			        var dotnetType = context.PredefinedTypes.GetDotNetType(parameter.Value.TypeRef.Symbol);
			        if (dotnetType != null)
			        {
				        result = Expression.Parameter(dotnetType, parameter.Value.Name.Text);
				        variablesMap[parameter.Symbol.Value] = result;
			        }
			        else
				        Debug.Assert(false, $".NET Type for {parameter.Value.TypeRef.Symbol} is null");
		        }
		        return result;
	        }

		    #endregion

			#region EmitStatement

			void EmitStatement(Statement statement)
	        {
		        switch (statement)
		        {
			        case Statement.Plain plainStmt:
						EmitTextWriteStatement<string>(plainStmt.Value.Value);
				        break;
			        case Statement.Output outputStmt:
				        EmitTextWriteStatement<string>(outputStmt.Expr.StringValue);
						break;
			        case SimpleLocalVariable simpleLocalVariableStmt:
				        break;
			        case CaptureLocalVariable captureLocalVariableStmt:
				        break;
			        case Statement.Unless unlessStmt:
				        break;
			        case Statement.If ifStmt:
				        break;
			        case ElseIf elseIfStmt:
				        break;
			        case Else elseStmt:
				        break;
			        case For forStmt:
				        break;
			        case Statement.Break breakStmt:
				        break;
			        case Statement.Continue continueStmt:
				        break;
					default:
						statements.Add(Expression.Empty());
						break;
				}
	        }

			#endregion

			#region EmitExpression

			Expression EmitExpression(Expr expression)
	        {
		        switch (expression)
		        {
			        case Expr.True trueExpr:
				        return Expression.Constant(true, typeof(bool));
			        case Expr.False falseExpr:
				        return Expression.Constant(false, typeof(bool)); ;
			        case Expr.Int intExpr:
				        return Expression.Constant(intExpr.Value.Value, typeof(int));
			        case Expr.Double doubleExpr:
				        return Expression.Constant(doubleExpr.Value.Value, typeof(double));
			        case Expr.SStr sStrExpr:
				        return Expression.Constant(sStrExpr.Value.Value, typeof(string));
			        case Expr.DStr dStrExpr:
				        return Expression.Constant(dStrExpr.Value.Value, typeof(string)); ;
			        case Expr.SimpleReference simpleReferenceExpr:
				        variablesMap.TryGetValue(simpleReferenceExpr.Ref.Symbol, out var value);
				        return value ?? Expression.Empty();
			        case Expr.MemberAccess memberAccessExpr:
				        break;
			        case Expr.ArrayAccess arrayAccessExpr:
				        break;
			        case Expr.Call callExpr:
				        break;
			        case Expr.Contains containsExpr:
				        break;
			        case Expr.And andExpr:
				        break;
			        case Expr.Or orExpr:
				        break;
			        case Expr.Equal equalExpr:
				        break;
			        case Expr.NotEqual notEqualExpr:
				        break;
			        case Expr.Range rangeExpr:
				        break;
			        case Expr.Error errorExpr:
				        break;
		        }
		        return Expression.Empty();
	        }

	        #endregion

			// the first parameter of tempalte method is TextWriter
	        var methodParameters = new List<ParameterExpression>{  textWriterParameter };

			var commentParameter = EmitCommentParameter();
			if(commentParameter != null)
				methodParameters.Add(commentParameter);

	        foreach (var statement in compilationUnit.Statements)
	        {
		        EmitStatement(statement);
	        }

			if(statements.Count == 0)
				statements.Add(Expression.Empty());

	        var templateMethod = Expression.Lambda(
				Expression.Block(
						//new []  { Expression.Variable(typeof(string), "igorvar") },
						statements
					),
				methodParameters
				);
	        templateMethod.CompileToMethod(methodBuilder);
        }

		private static MethodBuilder CreatreMethodBuilder(IAst ast, TypeBuilder tb)
		{
			var file = ast.Location.Source.File;
			var templateName = Path.GetFileNameWithoutExtension(file.Name).Replace(".", "_").Replace("-", "_");

			var methodBuilder = tb.DefineMethod(templateName, MethodAttributes.Public | MethodAttributes.Static, null, null);
			return methodBuilder;
		}
	}
}
