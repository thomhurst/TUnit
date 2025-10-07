using System.Text;
using TUnit.Assertions.Core;

namespace TUnit.Assertions.Conditions;

/// <summary>
/// Asserts that two objects are NOT structurally equivalent.
/// </summary>
public class NotStructuralEquivalencyAssertion<TValue> : Assertion<TValue>
{
    private readonly object? _notExpected;
    private bool _usePartialEquivalency;
    private readonly HashSet<string> _ignoredMembers = new();
    private readonly HashSet<Type> _ignoredTypes = new();

    public NotStructuralEquivalencyAssertion(
        EvaluationContext<TValue> context,
        object? notExpected,
        StringBuilder expressionBuilder)
        : base(context, expressionBuilder)
    {
        _notExpected = notExpected;
    }

    /// <summary>
    /// Enables partial equivalency mode where only properties/fields present on the expected object are compared.
    /// </summary>
    public NotStructuralEquivalencyAssertion<TValue> WithPartialEquivalency()
    {
        _usePartialEquivalency = true;
        ExpressionBuilder.Append(".WithPartialEquivalency()");
        return this;
    }

    /// <summary>
    /// Ignores a specific member path during equivalency comparison.
    /// </summary>
    public NotStructuralEquivalencyAssertion<TValue> IgnoringMember(string memberPath)
    {
        _ignoredMembers.Add(memberPath);
        ExpressionBuilder.Append($".IgnoringMember(\"{memberPath}\")");
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
        ExpressionBuilder.Append($".IgnoringType<{type.Name}>()");
        return this;
    }

    protected override Task<AssertionResult> CheckAsync(TValue? value, Exception? exception)
    {
        if (exception != null)
            return Task.FromResult(AssertionResult.Failed($"threw {exception.GetType().Name}: {exception.Message}"));

        // Create a temporary StructuralEquivalencyAssertion to reuse the comparison logic
        var tempAssertion = new StructuralEquivalencyAssertion<TValue>(
            Context,
            _notExpected,
            new StringBuilder());

        if (_usePartialEquivalency)
            tempAssertion.WithPartialEquivalency();

        foreach (var member in _ignoredMembers)
            tempAssertion.IgnoringMember(member);

        foreach (var type in _ignoredTypes)
            tempAssertion.IgnoringType(type);

        var result = tempAssertion.CompareObjects(value, _notExpected, "", new HashSet<object>(new ReferenceEqualityComparer()));

        // Invert the result - we want them to NOT be equivalent
        if (result.IsPassed)
            return Task.FromResult(AssertionResult.Failed("objects are equivalent but should not be"));

        return Task.FromResult(AssertionResult.Passed);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);
        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }

    protected override string GetExpectation() => "to not be equivalent";
}
