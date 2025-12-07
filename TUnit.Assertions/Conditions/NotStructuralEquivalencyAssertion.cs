using System.Diagnostics.CodeAnalysis;
using System.Text;
using TUnit.Assertions.Conditions.Helpers;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that two objects are NOT structurally equivalent.
/// </summary>
[RequiresUnreferencedCode("Uses reflection for structural equivalency comparison")]
public class NotStructuralEquivalencyAssertion<TValue> : Assertion<TValue>
{
    private readonly object? _notExpected;
    private readonly string? _notExpectedExpression;
    private bool _usePartialEquivalency;
    private readonly HashSet<string> _ignoredMembers = new();
    private readonly HashSet<Type> _ignoredTypes = new();

    public NotStructuralEquivalencyAssertion(
        AssertionContext<TValue> context,
        object? notExpected,
        string? notExpectedExpression = null)
        : base(context)
    {
        _notExpected = notExpected;
        _notExpectedExpression = notExpectedExpression;
    }

    /// <summary>
    /// Enables partial equivalency mode where only properties/fields present on the expected object are compared.
    /// </summary>
    public NotStructuralEquivalencyAssertion<TValue> WithPartialEquivalency()
    {
        _usePartialEquivalency = true;
        Context.ExpressionBuilder.Append(".WithPartialEquivalency()");
        return this;
    }

    /// <summary>
    /// Ignores a specific member path during equivalency comparison.
    /// </summary>
    public NotStructuralEquivalencyAssertion<TValue> IgnoringMember(string memberPath)
    {
        _ignoredMembers.Add(memberPath);
        Context.ExpressionBuilder.Append($".IgnoringMember(\"{memberPath}\")");
        return this;
    }

    /// <summary>
    /// Ignores all properties/fields of a specific type during equivalency comparison.
    /// </summary>
    public NotStructuralEquivalencyAssertion<TValue> IgnoringType<T>()
    {
        return IgnoringType(typeof(T));
    }

    /// <summary>
    /// Ignores all properties/fields of a specific type during equivalency comparison.
    /// </summary>
    public NotStructuralEquivalencyAssertion<TValue> IgnoringType(Type type)
    {
        _ignoredTypes.Add(type);
        Context.ExpressionBuilder.Append($".IgnoringType<{type.Name}>()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(EvaluationMetadata<TValue> metadata)
    {
        var value = metadata.Value;
        var exception = metadata.Exception;

        if (exception != null)
        {
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));
        }

        // Create a temporary StructuralEquivalencyAssertion to reuse the comparison logic
        var tempAssertion = new StructuralEquivalencyAssertion<TValue>(
            new AssertionContext<TValue>(Context.Evaluation, new StringBuilder()),
            _notExpected);

        if (_usePartialEquivalency)
        {
            tempAssertion.WithPartialEquivalency();
        }

        foreach (var member in _ignoredMembers)
            tempAssertion.IgnoringMember(member);

        foreach (var type in _ignoredTypes)
            tempAssertion.IgnoringType(type);

        var result = tempAssertion.CompareObjects(value, _notExpected, "", new HashSet<object>(ReferenceEqualityComparer<object>.Instance));

        // Invert the result - we want them to NOT be equivalent
        if (result.IsPassed)
        {
            return Task.FromResult(AssertionResult.Failed("objects are equivalent but should not be"));
        }

        return Task.FromResult(AssertionResult.Passed);
    }

    protected override string GetExpectation()
    {
        // Extract the source variable name from the expression builder
        // Format: "Assert.That(variableName).IsNotEquivalentTo(...)"
        var expressionString = Context.ExpressionBuilder.ToString();
        var sourceVariable = ExtractSourceVariable(expressionString);
        var notExpectedDesc = _notExpectedExpression ?? "expected value";

        return $"{sourceVariable} to not be equivalent to {notExpectedDesc}";
    }

    private static string ExtractSourceVariable(string expression)
    {
        // Extract variable name from "Assert.That(variableName)" or similar
        var thatIndex = expression.IndexOf(".That(");
        if (thatIndex >= 0)
        {
            var startIndex = thatIndex + 6; // Length of ".That("
            var endIndex = expression.IndexOf(')', startIndex);
            if (endIndex > startIndex)
            {
                var variable = expression.Substring(startIndex, endIndex - startIndex);
                // Handle lambda expressions like "async () => ..." by returning "value"
                if (variable.Contains("=>") || variable.StartsWith("()"))
                {
                    return "value";
                }
                return variable;
            }
        }

        return "value";
    }
}
