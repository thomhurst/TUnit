// Tool-local shims for the handful of TUnit.Core constants the linked engine sources
// reference. The real declarations live on types (TUnitActivitySource,
// ClassTimelineAttribute) whose files carry runtime behaviour the tool must not compile
// in, so only the constants are mirrored here. Values must match TUnit.Core exactly —
// they name span types and custom-property keys persisted in report sidecars.

namespace TUnit.Core;

/// <summary>Mirrors the span-type/tag constants from TUnit.Core\TUnitActivitySource.cs.</summary>
internal static class TUnitActivitySource
{
    internal const string SpanTestSession = "test session";
    internal const string SpanTestAssembly = "test assembly";
    internal const string SpanTestSuite = "test suite";
    internal const string SpanTestCase = "test case";
    internal const string SpanTestBody = "test body";
    internal const string TagTestClass = "tunit.test.class";
    internal const string TagTraceScope = "tunit.trace.scope";
}

/// <summary>Mirrors the property key from TUnit.Core\Attributes\TestMetadata\ClassTimelineAttribute.cs.</summary>
internal static class ClassTimelineAttribute
{
    internal const string ClassTimelinePropertyKey = "tunit.report.timeline";
}
