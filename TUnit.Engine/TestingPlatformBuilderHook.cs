using Microsoft.Testing.Platform.Builder;

namespace TUnit.Engine;

public static class TestingPlatformBuilderHook
{
    public static void AddExtensions(
        ITestApplicationBuilder testApplicationBuilder,
        string[] _) =>
        testApplicationBuilder.AddTUnit();
}