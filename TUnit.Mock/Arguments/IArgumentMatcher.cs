namespace TUnit.Mock.Arguments;

/// <summary>
/// Non-generic argument matcher interface used by the mock engine to test whether
/// an actual argument satisfies the expected condition.
/// </summary>
public interface IArgumentMatcher
{
    /// <summary>Determines whether the given value satisfies this matcher.</summary>
    /// <param name="value">The actual argument value passed to the mocked method.</param>
    /// <returns><see langword="true"/> if the value matches; otherwise <see langword="false"/>.</returns>
    bool Matches(object? value);

    /// <summary>Returns a human-readable description of this matcher for diagnostic messages.</summary>
    string Describe();
}

/// <summary>
/// Strongly-typed argument matcher that tests whether an actual argument of type <typeparamref name="T"/>
/// satisfies the expected condition.
/// </summary>
/// <typeparam name="T">The argument type.</typeparam>
public interface IArgumentMatcher<in T> : IArgumentMatcher
{
    /// <summary>Determines whether the given typed value satisfies this matcher.</summary>
    /// <param name="value">The actual argument value.</param>
    /// <returns><see langword="true"/> if the value matches; otherwise <see langword="false"/>.</returns>
    bool Matches(T? value);
}
