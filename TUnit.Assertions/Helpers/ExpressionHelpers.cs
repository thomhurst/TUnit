using System.Linq.Expressions;

namespace TUnit.Assertions.Helpers;

public static class ExpressionHelpers
{
    public static string GetName<T1, T2>(Expression<Func<T1, T2>> exp)
    {
        var body = exp.Body as MemberExpression;

        if (body == null)
        {
            var unaryExpression = (UnaryExpression)exp.Body;

            body = unaryExpression.Operand as MemberExpression;
        }

        return body!.Member.Name;
    }
}