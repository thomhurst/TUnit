using System.Collections.Concurrent;

namespace TUnit.Mocks.Arguments;

/// <summary>
/// Represents a typed argument matcher used in mock setup and verification expressions.
/// Implicitly converts from raw values for convenient inline usage.
/// Every Arg supports value capture — store it in a variable and read <see cref="Values"/>
/// after the mock is exercised.
/// </summary>
/// <typeparam name="T">The type of the argument being matched.</typeparam>
public readonly struct Arg<T>
{
    private readonly CapturingMatcher<T> _capturingMatcher;

    /// <summary>Gets the argument matcher. Public for generated code access. Not intended for direct use.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IArgumentMatcher Matcher => _capturingMatcher;

    /// <summary>Creates an Arg with a matcher. Public for generated code access. Not intended for direct use.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Arg(IArgumentMatcher matcher)
    {
        _capturingMatcher = new CapturingMatcher<T>(matcher);
    }

    /// <summary>Gets all captured values in order of capture.</summary>
    public IReadOnlyList<T?> Values => _capturingMatcher.CapturedValues;

    /// <summary>Gets the most recently captured value, or default if none captured.</summary>
    public T? Latest => _capturingMatcher.Latest;

    /// <summary>Implicitly converts a raw value to an <see cref="Arg{T}"/> using exact equality matching.</summary>
    /// <param name="value">The value to match against.</param>
    public static implicit operator Arg<T>(T value) => new(new TUnit.Mocks.Matchers.ExactMatcher<T>(value));
}
