using System.Linq.Expressions;
using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts on a member of an object selected via an expression.
/// This allows chaining assertions on object properties/fields.
/// </summary>
public class MemberAssertion<TObject, TMember> : Assertion<TMember>
{
    private readonly Expression<Func<TObject, TMember>> _memberSelector;
    private readonly string _memberPath;

    public MemberAssertion(
        EvaluationContext<TObject> parentContext,
        Expression<Func<TObject, TMember>> memberSelector,
        StringBuilder expressionBuilder)
        : base(
            parentContext.Map<TMember>(obj =>
            {
                if (obj == null)
                {
                    throw new InvalidOperationException($"Object `{typeof(TObject).Name}` was null");
                }

                var compiled = memberSelector.Compile();
                return compiled(obj);
            }),
            expressionBuilder)
    {
        _memberSelector = memberSelector;
        _memberPath = GetMemberPath(memberSelector);

        expressionBuilder.Append($".HasMember(x => x.{_memberPath})");
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TMember> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        // HasMember itself doesn't perform a check - it's a transformation
        // The actual check comes from the chained assertion (.EqualTo, etc.)
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed(exception.Message));

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation() => $"to have member {_memberPath}";

    private static string GetMemberPath(Expression<Func<TObject, TMember>> expression)
    {
        var body = expression.Body;
        var parts = new List<string>();

        while (body is MemberExpression memberExpr)
        {
            parts.Insert(0, memberExpr.Member.Name);
            body = memberExpr.Expression;
        }

        return parts.Count > 0 ? string.Join(".", parts) : "Unknown";
    }
}
