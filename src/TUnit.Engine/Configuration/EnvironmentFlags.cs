namespace TUnit.Engine.Configuration;

internal static class EnvironmentFlags
{
    /// <summary>
    /// The truthy vocabulary shared by the TUNIT_DISABLE_* switches: <c>1</c>, <c>true</c> or <c>yes</c>.
    /// </summary>
    public static bool IsTruthy(string? value)
        => value is not null &&
           (value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
            value.Equals("1", StringComparison.Ordinal) ||
            value.Equals("yes", StringComparison.OrdinalIgnoreCase));

    public static bool IsSet(string variableName)
        => IsTruthy(Environment.GetEnvironmentVariable(variableName));
}
