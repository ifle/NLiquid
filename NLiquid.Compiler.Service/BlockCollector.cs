using System.Collections.Generic;
using System.Linq.Expressions;

namespace NLiquid.Compiler.Service
{
	/// <summary>
	///
	/// </summary>
	public class BlockCollector
	{
		/// <summary>
		/// Gets the body.
		/// </summary>
		/// <value>
		/// The body.
		/// </value>
		public List<Expression> Body { get; } = new List<Expression>();
		/// <summary>
		/// Gets the variables.
		/// </summary>
		/// <value>
		/// The variables.
		/// </value>
		public List<ParameterExpression> Variables { get; } = new List<ParameterExpression>();

		/// <summary>
		/// Adds the expression.
		/// </summary>
		/// <param name="expression">The expression.</param>
		public BlockCollector AddExpression(Expression expression)
		{
			Body.Add(expression);
			return this;
		}

		/// <summary>
		/// Adds the expression.
		/// </summary>
		/// <param name="expression">The expression.</param>
		public BlockCollector AddVariable(ParameterExpression expression)
		{
			Variables.Add(expression);
			return this;
		}

		/// <summary>
		/// To the block expression.
		/// </summary>
		/// <returns></returns>
		public BlockExpression ToBlockExpression()
		{
			if(Body.Count == 0)
				Body.Add(Expression.Empty());
			if (Variables.Count > 0)
				return Expression.Block(Variables, Body);
			return Expression.Block(Body);
		}
	}
}