using JetBrains.ReSharper.Psi.JavaScript.Tree.TypeScript;

namespace ReSharper.ReJS.Shared
{
	public static class Ext
	{
		public static ITsExpression RightOperand(this ITsSimpleAssignmentExpression expression)
		{
			return expression.LastChild as ITsExpression;
		}

		public static ITsExpression LeftOperand(this ITsSimpleAssignmentExpression expression)
		{
			return expression.FirstChild as ITsExpression;
		}
	}
}
