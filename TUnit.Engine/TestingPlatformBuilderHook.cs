using System.Reflection;
using Microsoft.Testing.Platform.Builder;

namespace TUnit.Engine;

public static class TestingPlatformBuilderHook
{
#pragma warning disable IDE0060 // Remove unused parameter
    // ReSharper disable once UnusedParameter.Global
    public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] arguments)
#pragma warning restore IDE0060 // Remove unused parameter
    {
        testApplicationBuilder.AddTUnit(() => [Assembly.GetEntryAssembly()!]);
    }
}