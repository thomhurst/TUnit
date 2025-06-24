using Microsoft.Testing.Platform.Builder;
using TUnit.Engine.Extensions;

namespace TUnit.Engine.Framework;

public static class TestingPlatformBuilderHook
{
    public static void AddExtensions(
        ITestApplicationBuilder testApplicationBuilder,
        string[] _) =>
        testApplicationBuilder.AddTUnit();
}