using System;
using CliWrap;
using ModularPipelines.Context;
using ModularPipelines.DotNet.Extensions;
using ModularPipelines.DotNet.Options;
using ModularPipelines.Models;

namespace TUnit.Pipeline.Extensions;

/// <summary>
/// Custom extensions that suppress output for successful DotNet operations
/// </summary>
public static class QuietDotNetExtensions
{
    public static async Task<CommandResult> RunQuiet(this IDotNetContext dotNet, DotNetRunOptions options, CancellationToken cancellationToken = default)
    {
        // Build the command similar to how ModularPipelines does it
        var command = Cli.Wrap("dotnet")
            .WithArguments(BuildRunArguments(options))
            .WithWorkingDirectory(options.WorkingDirectory ?? Environment.CurrentDirectory);

        if (options.EnvironmentVariables?.Any() == true)
        {
            command = command.WithEnvironmentVariables(options.EnvironmentVariables);
        }

        // Execute with buffered output to suppress streaming to console
        var result = await command.ExecuteBufferedAsync(cancellationToken);

        // Only show output if the command failed
        if (result.ExitCode != 0)
        {
            Console.WriteLine(result.StandardOutput);
            Console.Error.WriteLine(result.StandardError);
        }

        return new CommandResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError,
            StartTime = result.StartTime,
            ExitTime = result.ExitTime
        };
    }

    public static async Task<CommandResult> TestQuiet(this IDotNetContext dotNet, DotNetTestOptions options, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("dotnet")
            .WithArguments(BuildTestArguments(options))
            .WithWorkingDirectory(options.WorkingDirectory ?? Environment.CurrentDirectory);

        if (options.EnvironmentVariables?.Any() == true)
        {
            command = command.WithEnvironmentVariables(options.EnvironmentVariables);
        }

        var result = await command.ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            Console.WriteLine(result.StandardOutput);
            Console.Error.WriteLine(result.StandardError);
        }

        return new CommandResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError,
            StartTime = result.StartTime,
            ExitTime = result.ExitTime
        };
    }

    public static async Task<CommandResult> PackQuiet(this IDotNetContext dotNet, DotNetPackOptions options, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("dotnet")
            .WithArguments(BuildPackArguments(options))
            .WithWorkingDirectory(options.WorkingDirectory ?? Environment.CurrentDirectory);

        if (options.EnvironmentVariables?.Any() == true)
        {
            command = command.WithEnvironmentVariables(options.EnvironmentVariables);
        }

        var result = await command.ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            Console.WriteLine(result.StandardOutput);
            Console.Error.WriteLine(result.StandardError);
        }

        return new CommandResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError,
            StartTime = result.StartTime,
            ExitTime = result.ExitTime
        };
    }

    public static async Task<CommandResult> NewQuiet(this IDotNetContext dotNet, DotNetNewOptions options, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("dotnet")
            .WithArguments(BuildNewArguments(options))
            .WithWorkingDirectory(options.WorkingDirectory ?? Environment.CurrentDirectory);

        if (options.EnvironmentVariables?.Any() == true)
        {
            command = command.WithEnvironmentVariables(options.EnvironmentVariables);
        }

        var result = await command.ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0 || !options.ThrowOnNonZeroExitCode)
        {
            Console.WriteLine(result.StandardOutput);
            Console.Error.WriteLine(result.StandardError);
        }

        return new CommandResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError,
            StartTime = result.StartTime,
            ExitTime = result.ExitTime
        };
    }

    public static async Task<CommandResult> NugetAddSourceQuiet(this IDotNetContext dotNet, DotNetNugetAddSourceOptions options, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("dotnet")
            .WithArguments(BuildNugetAddSourceArguments(options))
            .WithWorkingDirectory(options.WorkingDirectory ?? Environment.CurrentDirectory);

        if (options.EnvironmentVariables?.Any() == true)
        {
            command = command.WithEnvironmentVariables(options.EnvironmentVariables);
        }

        var result = await command.ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            Console.WriteLine(result.StandardOutput);
            Console.Error.WriteLine(result.StandardError);
        }

        return new CommandResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError,
            StartTime = result.StartTime,
            ExitTime = result.ExitTime
        };
    }

    public static async Task<CommandResult> NugetPushQuiet(this IDotNetContext dotNet, DotNetNugetPushOptions options, CancellationToken cancellationToken = default)
    {
        var command = Cli.Wrap("dotnet")
            .WithArguments(BuildNugetPushArguments(options))
            .WithWorkingDirectory(options.WorkingDirectory ?? Environment.CurrentDirectory);

        if (options.EnvironmentVariables?.Any() == true)
        {
            command = command.WithEnvironmentVariables(options.EnvironmentVariables);
        }

        var result = await command.ExecuteBufferedAsync(cancellationToken);

        if (result.ExitCode != 0)
        {
            Console.WriteLine(result.StandardOutput);
            Console.Error.WriteLine(result.StandardError);
        }

        return new CommandResult
        {
            ExitCode = result.ExitCode,
            StandardOutput = result.StandardOutput,
            StandardError = result.StandardError,
            StartTime = result.StartTime,
            ExitTime = result.ExitTime
        };
    }

    private static string[] BuildRunArguments(DotNetRunOptions options)
    {
        var args = new List<string> { "run" };
        
        if (!string.IsNullOrEmpty(options.Project))
            args.AddRange(["--project", options.Project]);
        
        if (options.Configuration.HasValue)
            args.AddRange(["--configuration", options.Configuration.Value.ToString()]);
        
        if (!string.IsNullOrEmpty(options.Framework))
            args.AddRange(["--framework", options.Framework]);
        
        if (options.NoBuild)
            args.Add("--no-build");
        
        if (options.Arguments?.Any() == true)
        {
            args.Add("--");
            args.AddRange(options.Arguments);
        }
        
        return args.ToArray();
    }

    private static string[] BuildTestArguments(DotNetTestOptions options)
    {
        var args = new List<string> { "test" };
        
        if (options.Project != null)
            args.Add(options.Project.FullName);
        
        if (options.Configuration.HasValue)
            args.AddRange(["--configuration", options.Configuration.Value.ToString()]);
        
        if (!string.IsNullOrEmpty(options.Framework))
            args.AddRange(["--framework", options.Framework]);
        
        if (options.NoBuild)
            args.Add("--no-build");
        
        return args.ToArray();
    }

    private static string[] BuildPackArguments(DotNetPackOptions options)
    {
        var args = new List<string> { "pack" };
        
        if (options.Project != null)
            args.Add(options.Project.FullName);
        
        if (options.Configuration.HasValue)
            args.AddRange(["--configuration", options.Configuration.Value.ToString()]);
        
        if (options.IncludeSource == true)
            args.Add("--include-source");
        
        if (options.Properties?.Any() == true)
        {
            foreach (var prop in options.Properties)
            {
                args.AddRange(["-p", $"{prop.Key}={prop.Value}"]);
            }
        }
        
        return args.ToArray();
    }

    private static string[] BuildNewArguments(DotNetNewOptions options)
    {
        var args = new List<string> { "new", options.Template };
        
        if (!string.IsNullOrEmpty(options.Name))
            args.AddRange(["--name", options.Name]);
        
        if (options.Arguments?.Any() == true)
            args.AddRange(options.Arguments);
        
        return args.ToArray();
    }

    private static string[] BuildNugetAddSourceArguments(DotNetNugetAddSourceOptions options)
    {
        var args = new List<string> { "nuget", "add", "source" };
        
        if (!string.IsNullOrEmpty(options.Source))
            args.Add(options.Source);
        
        if (!string.IsNullOrEmpty(options.Name))
            args.AddRange(["--name", options.Name]);
        
        return args.ToArray();
    }

    private static string[] BuildNugetPushArguments(DotNetNugetPushOptions options)
    {
        var args = new List<string> { "nuget", "push" };
        
        if (options.Package != null)
            args.Add(options.Package.FullName);
        
        if (!string.IsNullOrEmpty(options.Source))
            args.AddRange(["--source", options.Source]);
        
        if (!string.IsNullOrEmpty(options.ApiKey))
            args.AddRange(["--api-key", options.ApiKey]);
        
        return args.ToArray();
    }
}