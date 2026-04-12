namespace TUnit.Core.Settings;

/// <summary>
/// Controls visual output settings.
/// </summary>
public sealed class DisplaySettings
{
    /// <summary>
    /// Whether to show full stack traces including TUnit internals. Default: <c>false</c>.
    /// Precedence: <c>--detailed-stacktrace</c> → TUnitSettings → built-in default.
    /// </summary>
    public bool DetailedStackTrace { get; set; }
}
