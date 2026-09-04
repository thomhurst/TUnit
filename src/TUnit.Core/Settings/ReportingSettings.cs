namespace TUnit.Core.Settings;

/// <summary>
/// Controls built-in report generation and publishing.
/// </summary>
public sealed class ReportingSettings
{
    internal ReportingSettings() { }

    /// <summary>
    /// Whether to generate the HTML test report. Default: <c>true</c>.
    /// Precedence: <c>TUNIT_DISABLE_HTML_REPORTER</c> → TUnitSettings → built-in default.
    /// </summary>
    public bool HtmlReportEnabled { get; set; } = true;

    /// <summary>
    /// Whether to generate the machine-readable JSON report sidecar. Default: <c>true</c>.
    /// Precedence: <c>TUNIT_DISABLE_JSON_REPORT</c> → TUnitSettings → built-in default.
    /// </summary>
    public bool JsonReportEnabled { get; set; } = true;

    /// <summary>
    /// Whether to upload the HTML report as an artifact when supported by the CI environment.
    /// Default: <c>true</c>.
    /// Precedence: <c>TUNIT_DISABLE_ARTIFACT_UPLOAD</c> → TUnitSettings → built-in default.
    /// </summary>
    public bool ArtifactUploadEnabled { get; set; } = true;
}
