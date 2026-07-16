namespace TUnit.Core;

// Constants-only partial, source-linked into TUnit.Reporting.Tool (the linked
// HtmlReportGenerator reads the property key back out of report sidecars). Keeping the
// key here — away from the attribute's behaviour and its base types — lets the tool
// compile just this file, so the engine and the tool can never disagree on the value.
public sealed partial class ClassTimelineAttribute
{
    /// <summary>
    /// Custom-property key used to round-trip the chosen <see cref="TUnit.Core.Enums.TimelineMode"/> into
    /// <c>TestDetails.CustomProperties</c> so the HTML reporter can read it back per class.
    /// </summary>
    internal const string ClassTimelinePropertyKey = "tunit.report.timeline";
}
