using System;
using System.CodeDom;
using System.Collections;
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
using NLiquid.Filters;
using NLiquid.Parser.Ast;

namespace NLiquid.Compiler.Service
{
	public class Compiler : IPostprocessing
	{
		private static MethodInfo _defaultTextWriteMethodInfo = typeof(TextWriter).GetMethod("WriteLine", new[] { typeof(object) });
		private static ParameterExpression _textWriterParameter = Expression.Parameter(typeof(TextWriter), "writer");
		private static Dictionary<System.Type, MethodInfo> _writeTextWriteMethodInfoMap = new Dictionary<System.Type, MethodInfo>
		{
			{ typeof(int),     typeof(TextWriter).GetMethod("Write", new[] { typeof(int) }) },
			{ typeof(double),  typeof(TextWriter).GetMethod("Write", new[] { typeof(double) }) },
			{ typeof(string),  typeof(TextWriter).GetMethod("Write", new[] { typeof(string) }) },
			{ typeof(bool),    typeof(TextWriter).GetMethod("Write", new[] { typeof(bool) }) },
		};

		NLiquidDependentPropertyEvalContext _context;
		Dictionary<LocalVariableSymbol,Expression> _variablesMap;

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
	        _variablesMap				   = new Dictionary<LocalVariableSymbol, Expression>();
	        _context                       = context;
	        //_variablesMap				   = new Dictionary<LocalVariableSymbol, Expression>(context.PredefinedTypes.DotNetTypesMap);
			// the first parameter of tempalte method is TextWriter
	        var methodParameters = new BlockCollector();
	        methodParameters.AddVariable(_textWriterParameter);

			var commentParameter = EmitCommentParameter(compilationUnit);
			if(commentParameter != null)
				methodParameters.AddVariable(commentParameter);

			var blockCollector = new BlockCollector();
	        foreach (var statement in compilationUnit.Statements)
	        {
		        EmitStatement(statement, blockCollector);
	        }

	        var templateMethod = Expression.Lambda(blockCollector.ToBlockExpression(), methodParameters.Variables);
	        templateMethod.CompileToMethod(methodBuilder);
        }

		private static MethodBuilder CreatreMethodBuilder(IAst ast, TypeBuilder tb)
		{
			var file = ast.Location.Source.File;
			var templateName = Path.GetFileNameWithoutExtension(file.Name).Replace(".", "_").Replace("-", "_");

			var methodBuilder = tb.DefineMethod(templateName, MethodAttributes.Public | MethodAttributes.Static, null, null);
			return methodBuilder;
		}

		private ParameterExpression EmitCommentParameter(CompilationUnit compilationUnit)
		{
			ParameterExpression result = null;
			var parameter = compilationUnit.Parameter;
			if (parameter.HasValue)
			{
				var dotnetType = _context.PredefinedTypes.GetDotNetType(parameter.Value.TypeRef.Symbol);
				if (dotnetType != null)
				{
					result = Expression.Parameter(dotnetType, parameter.Value.Name.Text);
					_variablesMap[parameter.Symbol.Value] = result;
				}
				else
					Debug.Assert(false, $".NET Type for {parameter.Value.TypeRef.Symbol} is null");
			}
			return result;
		}

		private Expression EmitExpression(Expr expression)
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
					_variablesMap.TryGetValue(simpleReferenceExpr.Ref.Symbol, out var value);
					return value ?? Expression.Empty();
				case Expr.MemberAccess memberAccessExpr:
					return Expression.Property(EmitExpression(memberAccessExpr.Qualifier), memberAccessExpr.PropertyRef.Name);
				case Expr.ArrayAccess arrayAccessExpr:
					_variablesMap.TryGetValue(arrayAccessExpr.Ref.Symbol, out var arrValue);
					return Expression.ArrayAccess(arrValue, EmitExpression(arrayAccessExpr.Index));
				case Expr.Call callExpr:
					return EmitCallExpression(callExpr);
				case Expr.Contains containsExpr:
					break;
				case Expr.And andExpr:
					return Expression.And(EmitExpression(andExpr.Expr1), EmitExpression(andExpr.Expr2));
				case Expr.Or orExpr:
					return Expression.And(EmitExpression(orExpr.Expr1), EmitExpression(orExpr.Expr2));
				case Expr.Equal equalExpr:
					return Expression.Equal(EmitExpression(equalExpr.Expr1), EmitExpression(equalExpr.Expr2));
				case Expr.NotEqual notEqualExpr:
					return Expression.NotEqual(EmitExpression(notEqualExpr.Expr1), EmitExpression(notEqualExpr.Expr2));
				case Expr.Range rangeExpr:
					break;
				case Expr.Error errorExpr:
					break;
			}
			return Expression.Empty();
		}

		private BlockCollector EmitStatement(IEnumerable<Statement> statements, BlockCollector blockCollector = null)
		{
			if(blockCollector == null)
				blockCollector = new BlockCollector();
			foreach (var statement in statements)
			{
				EmitStatement(statement, blockCollector);
			}

			return blockCollector;
		}

		private void EmitStatement(Statement statement, BlockCollector blockCollector)
		{
			switch (statement)
			{
				case Statement.Plain plainStmt:
					EmitTextWriteStatement<string>(plainStmt.Value.Value, blockCollector);
					break;
				case Statement.Output outputStmt:
					EmitOutputStatement(outputStmt, blockCollector);
					break;
				case SimpleLocalVariable simpleLocalVariableStmt:
					EmitSimpleLocalVariableStatement(simpleLocalVariableStmt, blockCollector);
					break;
				case CaptureLocalVariable captureLocalVariableStmt:
					break;
				case Statement.Unless unlessStmt:
					break;
				case Statement.If ifStmt:
					EmitIfStatement(ifStmt, blockCollector);
					break;
				case For forStmt:
					EmitForStatement(forStmt, blockCollector);
					break;
				case Statement.Break breakStmt:
					//blockCollector.AddExpression(Expression.Return(Expression.la))
					break;
				case Statement.Continue continueStmt:
					break;
				default:
					blockCollector.AddExpression(Expression.Empty());
					break;
			}
		}

		private void EmitOutputStatement(Statement.Output outputStmt, BlockCollector blockCollector)
		{
			var valExpr = EmitExpression(outputStmt.Expr);


			var valueType = valExpr.Type;

			if (!_writeTextWriteMethodInfoMap.TryGetValue(valueType, out var methodInfo))
				methodInfo = _defaultTextWriteMethodInfo;
			blockCollector.AddExpression(Expression.Call(_textWriterParameter, methodInfo, valExpr));
		}

		private void EmitTextWriteStatement<T>(T value, BlockCollector blockCollector)
		{
			var valueType = typeof(T);

			if (!_writeTextWriteMethodInfoMap.TryGetValue(valueType, out var methodInfo))
				methodInfo = _defaultTextWriteMethodInfo;
			blockCollector.AddExpression(Expression.Call(_textWriterParameter, methodInfo, Expression.Constant(value, valueType)));
		}

		private void EmitSimpleLocalVariableStatement(SimpleLocalVariable simpleLocalVariableStmt, BlockCollector blockCollector)
		{
			if(_context.PredefinedTypes.DotNetTypesMap.TryGetValue(simpleLocalVariableStmt.Symbol.Type, out var type))
			{
				var variable = Expression.Variable(type, simpleLocalVariableStmt.Name.Text);
				var assingExpr = Expression.Assign(variable, EmitExpression(simpleLocalVariableStmt.Initializer));
				blockCollector.AddVariable(variable).AddExpression(assingExpr);

				_variablesMap[simpleLocalVariableStmt.Symbol] = variable;
			}

		}

		private void EmitIfStatement(Statement.If ifStmt, BlockCollector blockCollector)
		{
			var ifStmtElseIfs     = (IAstList<ElseIf>) ifStmt.ElseIfs;
			var elseBody          = ifStmt.Else.HasValue ? EmitStatement(ifStmt.Else.Value.Body).ToBlockExpression() : null;
			Expression elseIfExpr = null;
			// else if statements
			if(ifStmt.ElseIfs != null && ifStmtElseIfs.Count > 0)
			{
				var lastElseIf = ifStmtElseIfs[ifStmtElseIfs.Count - 1];
				Expression lastElseIfExpr = elseBody == null
					? Expression.IfThen(EmitExpression(lastElseIf.Condition), EmitStatement(lastElseIf.Body).ToBlockExpression())
					: Expression.IfThenElse(EmitExpression(lastElseIf.Condition), EmitStatement(lastElseIf.Body).ToBlockExpression(), elseBody);

				for (int i = ifStmtElseIfs.Count - 2; i >= 0; i--)
				{
					var currentElseIf = ifStmtElseIfs[i];
					elseIfExpr = Expression.IfThenElse(
												EmitExpression(currentElseIf.Condition), EmitStatement(currentElseIf.Body).ToBlockExpression(),
												Expression.Block(lastElseIfExpr));
					lastElseIfExpr = elseIfExpr;
				}

				elseBody = Expression.Block(elseIfExpr ?? lastElseIfExpr);
			}

			blockCollector.AddExpression(elseBody == null
				? Expression.IfThen(EmitExpression(ifStmt.Condition), EmitStatement(ifStmt.Body).ToBlockExpression())
				: Expression.IfThenElse(EmitExpression(ifStmt.Condition), EmitStatement(ifStmt.Body).ToBlockExpression(), elseBody));
		}

		private void EmitForStatement(For forStmtFor, BlockCollector blockCollector)
		{
			var forBlockCollector = new BlockCollector();
			var collectionExpr = EmitExpression(forStmtFor.ForSource);
			var elementType = _context.PredefinedTypes.GetDotNetType(forStmtFor.Symbol.Type);
			var enumerableType = typeof(IEnumerable<>).MakeGenericType(elementType);
			var enumeratorType = typeof(IEnumerator<>).MakeGenericType(elementType);

			var enumeratorVar = Expression.Variable(enumeratorType, "enumerator");
			var getEnumeratorCall = Expression.Call(collectionExpr, enumerableType.GetMethod("GetEnumerator", BindingFlags.Instance | BindingFlags.Public));
			var enumeratorAssign = Expression.Assign(enumeratorVar, getEnumeratorCall);
			blockCollector.AddVariable(enumeratorVar);
			// The MoveNext method's actually on IEnumerator, not IEnumerator<T>
			var moveNextCall = Expression.Call(enumeratorVar, typeof(IEnumerator).GetMethod("MoveNext", BindingFlags.Instance | BindingFlags.Public));
			var breakLabel = Expression.Label("LoopBreak");

			var loopVar = Expression.Variable(elementType, forStmtFor.Name.Text);
			forBlockCollector.AddVariable(loopVar);
			_variablesMap[forStmtFor.Symbol] = loopVar;
			forBlockCollector.AddExpression(Expression.Assign(loopVar, Expression.Property(enumeratorVar, "Current")));
			EmitStatement(forStmtFor.Body, forBlockCollector);

			var loop = Expression.Block(new[] { enumeratorVar },
				enumeratorAssign,
				Expression.Loop(
					Expression.IfThenElse(
						Expression.Equal(moveNextCall, Expression.Constant(true)),
						forBlockCollector.ToBlockExpression(),
						Expression.Break(breakLabel)
					),
					breakLabel)
			);

			blockCollector.AddExpression(loop);
		}

		private Expression EmitCallExpression(Expr.Call call)
		{
			if (call.FuncName.Ref.Symbol.FirstDeclarationOrDefault is FilterFunDeclaration filterDeclaration)
			{
				var args = new [] { EmitExpression(call.Arg0) }.Concat(call.Args.Select(expr => EmitExpression(expr))).ToArray();
				if(args.Length > 0)
					return Expression.Call(filterDeclaration.Method, args);
				return Expression.Call(filterDeclaration.Method);
			}

			return Expression.Empty();
		}
	}
}
