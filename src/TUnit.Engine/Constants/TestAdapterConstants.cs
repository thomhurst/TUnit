namespace TUnit.Engine.Constants;

internal static class TestAdapterConstants
{
    internal const string ExecutorUriString = "executor://tunit/TestRunner/net";
    internal static readonly Uri ExecutorUri = new(ExecutorUriString);
}
