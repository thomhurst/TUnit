#if NET

namespace TUnit.Core;

// Span/tag constants consumed by the HTML report pipeline (HtmlReportGenerator). They live
// in this constants-only partial so TUnit.Reporting.Tool can source-link exactly these
// values without dragging in the ActivitySource statics — which also makes value drift
// between the engine and the tool impossible. Keep this file free of anything but consts.
public static partial class TUnitActivitySource
{
    // Span names used across the engine and HTML report.
    internal const string SpanTestSession = "test session";
    internal const string SpanTestAssembly = "test assembly";
    internal const string SpanTestSuite = "test suite";
    internal const string SpanTestCase = "test case";
    internal const string SpanTestBody = "test body";

    internal const string TagTestClass = "tunit.test.class";
    internal const string TagTestSuiteName = "test.suite.name";
    internal const string TagTraceScope = "tunit.trace.scope";
}

#endif
