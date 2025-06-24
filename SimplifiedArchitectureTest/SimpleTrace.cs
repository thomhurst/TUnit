using System;
using System.Threading.Tasks;
using Microsoft.Testing.Platform.Builder;
using TUnit.Engine.Extensions;

namespace SimplifiedArchitectureTest;

public static class SimpleTrace
{
    public static async Task<int> RunWithTrace(string[] args)
    {
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Creating test application builder...");
        var builder = await TestApplication.CreateBuilderAsync(args);
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Adding SimplifiedTUnit...");
        builder.AddSimplifiedTUnit();
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Building application...");
        using var app = await builder.BuildAsync();
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Running application...");
        var exitCode = await app.RunAsync();
        
        Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] RunAsync completed with exit code: {exitCode}");
        
        return exitCode;
    }
}