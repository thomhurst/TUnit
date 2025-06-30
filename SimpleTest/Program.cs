using System;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Builder;
using TUnit.Engine;
using TUnit.Engine.Extensions;

// Entry point for TUnit test runner
class Program
{
    static async Task<int> Main(string[] args)
    {
        var builder = await TestApplication.CreateBuilderAsync(args);
        builder.AddTUnit();
        using var app = await builder.BuildAsync();
        return await app.RunAsync();
    }
}