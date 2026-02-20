namespace TUnit.Mock.Arguments;

/// <summary>
/// Represents a typed argument matcher used in mock setup and verification expressions.
/// Implicitly converts from raw values for convenient inline usage.
/// </summary>
/// <typeparam name="T">The type of the argument being matched.</typeparam>
public readonly struct Arg<T>
{
    /// <summary>Gets the argument matcher. Public for generated code access. Not intended for direct use.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public IArgumentMatcher Matcher { get; }

    /// <summary>Creates an Arg with a matcher. Public for generated code access. Not intended for direct use.</summary>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public Arg(IArgumentMatcher matcher) => Matcher = matcher;

    /// <summary>Implicitly converts a raw value to an <see cref="Arg{T}"/> using exact equality matching.</summary>
    /// <param name="value">The value to match against.</param>
    public static implicit operator Arg<T>(T value) => new(new TUnit.Mock.Matchers.ExactMatcher<T>(value));
}
