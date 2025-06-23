namespace TUnit.Core.Interfaces;

/// <summary>
/// Service responsible for formatting test names and argument values for display.
/// </summary>
public interface ITestNameFormatter
{
    /// <summary>
    /// Formats a test name template with the provided arguments.
    /// </summary>
    /// <param name="template">The name template, which may contain placeholders</param>
    /// <param name="classArgs">Class constructor arguments</param>
    /// <param name="methodArgs">Test method arguments</param>
    /// <param name="propertyValues">Property values for data injection</param>
    /// <returns>The formatted test name</returns>
    string FormatTestName(
        string template, 
        object?[]? classArgs = null, 
        object?[]? methodArgs = null,
        IDictionary<string, object?>? propertyValues = null);

    /// <summary>
    /// Formats a single argument value for display in test names.
    /// </summary>
    /// <param name="value">The value to format</param>
    /// <returns>A string representation suitable for display</returns>
    string FormatArgumentValue(object? value);

    /// <summary>
    /// Builds a test ID by replacing placeholders in the template.
    /// </summary>
    /// <param name="template">The ID template with placeholders like {TestIndex}</param>
    /// <param name="testIndex">The test index</param>
    /// <param name="repeatIndex">The repeat index</param>
    /// <param name="classDataIndex">The class data index</param>
    /// <param name="methodDataIndex">The method data index</param>
    /// <returns>The formatted test ID</returns>
    string BuildTestId(
        string template, 
        int testIndex, 
        int repeatIndex = 0, 
        int classDataIndex = 0, 
        int methodDataIndex = 0);
}