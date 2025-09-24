namespace TUnit.Assertions.AssertionBuilders.Interfaces;

/// <summary>
/// Internal interface for formatting assertion expressions
/// </summary>
internal interface IExpressionFormatter
{
    /// <summary>
    /// Gets the actual expression that started the assertion
    /// </summary>
    string? ActualExpression { get; }

    /// <summary>
    /// Gets the full formatted expression
    /// </summary>
    string? GetExpression();

    /// <summary>
    /// Appends a connector (And, Or) to the expression
    /// </summary>
    void AppendConnector(string connector);

    /// <summary>
    /// Appends a method call to the expression
    /// </summary>
    void AppendMethod(string methodName, string?[] arguments);
}