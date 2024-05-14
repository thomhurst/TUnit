using Microsoft.Testing.Platform.Builder;

namespace TUnit.Engine;

public static class TUnitRunner
{
    public static async Task<int> RunTests(params string[] args)
    {
        var builder = await TestApplication.CreateBuilderAsync(args);
        TestingPlatformBuilderHook.AddExtensions(builder, []);
        var app = await builder.BuildAsync();
        return await app.RunAsync();
    }
}