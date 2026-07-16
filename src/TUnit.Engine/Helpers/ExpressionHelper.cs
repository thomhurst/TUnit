using System.Linq.Expressions;
using System.Reflection;

namespace TUnit.Engine.Helpers;

internal static class ExpressionHelper
{
    /// <summary>
    /// Extracts the test <see cref="MethodInfo"/> from a dynamic test expression tree.
    /// Handles all expression shapes produced by CreateTestVariantInternal:
    /// <list type="bullet">
    ///   <item>Task-returning: direct MethodCallExpression</item>
    ///   <item>void-returning: BlockExpression containing the call</item>
    ///   <item>Task&lt;T&gt;-returning: UnaryExpression (Convert) wrapping the call</item>
    ///   <item>ValueTask-returning: MethodCallExpression wrapping AsTask() on the call</item>
    ///   <item>ValueTask&lt;T&gt;-returning: UnaryExpression wrapping AsTask() on the call</item>
    /// </list>
    /// </summary>
    public static MethodInfo ExtractMethodInfo(Expression? testMethod)
    {
        var lambdaExpression = testMethod as LambdaExpression;

        var methodCall = lambdaExpression?.Body switch
        {
            MethodCallExpression mc => mc,
            UnaryExpression { Operand: MethodCallExpression umc } => umc,
            BlockExpression block => FindMethodCall(block),
            _ => null,
        };

        // Unwrap wrapper calls like ValueTask.AsTask() to find the actual test method call.
        // The test method call has a ParameterExpression as its Object (the test instance).
        while (methodCall is { Object: MethodCallExpression inner })
        {
            methodCall = inner;
        }

        return methodCall?.Method
            ?? throw new InvalidOperationException("Could not extract method info from dynamic test expression");
    }

    // Void-returning block shape: Block(Call(instance, testMethod), Constant(Task.CompletedTask))
    // The test method call is always the first MethodCallExpression in the block.
    private static MethodCallExpression? FindMethodCall(BlockExpression blockExpression)
    {
        foreach (var expr in blockExpression.Expressions)
        {
            if (expr is MethodCallExpression methodCall)
            {
                return methodCall;
            }
        }

        return null;
    }
}
