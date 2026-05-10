namespace TUnit.Core.Settings;

/// <summary>
/// Controls HTML report rendering. Independent from <see cref="DisplaySettings"/>, which
/// governs console output.
/// </summary>
public sealed class ReportSettings
{
    internal ReportSettings() { }

    /// <summary>
    /// When <c>true</c>, the HTML report's class timeline includes each test-case span and
    /// its non-<c>test body</c> children, making BDD-style <c>[DependsOn]</c> chains visible
    /// at the class level. When <c>false</c> (default), the class timeline shows only
    /// class-level infrastructure spans (suite, init/dispose) — quieter for classes of
    /// independent tests.
    /// </summary>
    public bool ExpandClassTimeline { get; set; }
}
