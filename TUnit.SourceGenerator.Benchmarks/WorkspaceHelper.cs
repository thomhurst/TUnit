using System.Runtime.CompilerServices;
using BenchmarkDotNet.Loggers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace TUnit.SourceGenerator.Benchmarks;

public static class WorkspaceHelper
{
    private static string GetDirectoryRelativePath(string projectPath, [CallerFilePath] string callerFilePath = null!) =>
        Path.Combine(Path.GetDirectoryName(callerFilePath)!, projectPath);

    public static async Task<(Compilation, CSharpGeneratorDriver, MSBuildWorkspace)> SetupAsync<TSourceGenerator>( string projectPath)
        where TSourceGenerator : IIncrementalGenerator, new()
    {
        MSBuildWorkspace workspace = MSBuildWorkspace.Create();
        workspace.RegisterWorkspaceFailedHandler(args =>
        {
            ConsoleLogger.Default.WriteLineError("-------------------------");
            ConsoleLogger.Default.WriteLineError(args.Diagnostic.ToString());
            ConsoleLogger.Default.WriteLineError("-------------------------");
        });

        var projectFile = GetDirectoryRelativePath(projectPath);

        if (!File.Exists(projectFile))
            throw new Exception($"Project doesn't exist at {projectFile}");

        ConsoleLogger.Default.WriteLine($"Project exists at {projectFile}");

        Project project;
        try
        {
            ConsoleLogger.Default.WriteLine("Loading project\n");
            project = await workspace.OpenProjectAsync(projectFile);
            ConsoleLogger.Default.WriteLine("\nLoaded project");
        }
        catch (Exception ex)
        {
            ConsoleLogger.Default.WriteError($"Error: {ex.Message}");
            throw;
        }

        var compilation = await project.GetCompilationAsync();
        if (compilation == null)
            throw new InvalidOperationException("Compilation returned null");

        var generator = new TSourceGenerator().AsSourceGenerator();

        var driver =
            CSharpGeneratorDriver.Create([generator], parseOptions: (CSharpParseOptions) project.ParseOptions!);

        return (compilation, driver, workspace);
    }
}
