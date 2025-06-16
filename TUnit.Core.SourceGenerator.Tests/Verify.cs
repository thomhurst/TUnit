global using static TUnit.Core.SourceGenerator.Tests.VerifyTUnit;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace TUnit.Core.SourceGenerator.Tests;

public sealed class VerifySettingsTask
{
    private object? _value;
    private readonly List<Func<StringBuilder, StringBuilder>> _scrubbers = [];
    private readonly List<Func<string, string>> _lineScrubbers = [];
    private Func<(string ReceivedPath, string VerifiedPath), string, Func<Task>, Task>? _onVerifyMismatch;
    private string? _uniqueSuffix;
    private string? _receivedPath;
    private string? _verifiedPath;

    public VerifySettingsTask(object? value)
    {
        _value = value;
    }

    public VerifySettingsTask AddScrubber(Func<StringBuilder, StringBuilder> scrubber)
    {
        _scrubbers.Add(scrubber);
        return this;
    }

    public VerifySettingsTask ScrubLinesWithReplace(Func<string, string> lineScrubber)
    {
        _lineScrubbers.Add(lineScrubber);
        return this;
    }

    public VerifySettingsTask OnVerifyMismatch(Func<(string ReceivedPath, string VerifiedPath), string, Func<Task>, Task> handler)
    {
        _onVerifyMismatch = handler;
        return this;
    }

    public VerifySettingsTask UniqueForTargetFrameworkAndVersion(Assembly? assembly)
    {
#if NETFRAMEWORK
        _uniqueSuffix = ".net472";
#else
        _uniqueSuffix = $".net{Environment.Version.Major}.{Environment.Version.Minor}";
#endif

        return this;
    }

    public VerifySettingsTask UniqueForTargetFrameworkAndVersion()
    {
        return UniqueForTargetFrameworkAndVersion(typeof(VerifySettingsTask).Assembly);
    }

    private static string ConvertToShortFrameworkName(string frameworkName)
    {
        // Parse the framework name like ".NETStandard,Version=v2.0"
        var parts = frameworkName.Split(',');
        if (parts.Length < 2)
            return frameworkName;

        var framework = parts[0].Trim();
        var versionPart = parts[1].Trim();

        // Extract version number
        var versionMatch = System.Text.RegularExpressions.Regex.Match(versionPart, @"Version=v?(\d+\.\d+(?:\.\d+)?)");
        if (!versionMatch.Success)
            return frameworkName;

        var version = versionMatch.Groups[1].Value;

        // Convert to short form
        return framework switch
        {
            ".NETStandard" => $"netstandard{version}",
            ".NETCoreApp" => version.StartsWith("3.") || version == "3.0" || version == "3.1"
                ? $"netcoreapp{version}"
                : $"net{version}",
            ".NETFramework" => $"net{version.Replace(".", "")}",
            _ => $"{framework.ToLowerInvariant()}{version}"
        };
    }

    public VerifySettingsTask ScrubLinesContaining(string substring)
    {
        _scrubbers.Add(sb =>
        {
            var lines = sb.ToString().Split('\n');
            var filtered = lines.Where(line => !line.Contains(substring)).ToArray();
            return new StringBuilder(string.Join("\n", filtered));
        });
        return this;
    }

    public async Task ToTask()
    {
        var testContext = TestContext.Current!;
        var testClassName = testContext.TestDetails.TestClass.Name;
        var testName = testContext.TestDetails.TestName;
        var name = $"{testClassName}.{testName}{_uniqueSuffix}";
        var dir = Directory.GetCurrentDirectory();
        _receivedPath = Path.Combine(dir, $"{name}.received.txt");
        _verifiedPath = Path.Combine(dir, $"{name}.verified.txt");

        var serialized = _value as string ?? JsonSerializer.Serialize(_value, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        var sb = new StringBuilder(serialized);
        foreach (var scrubber in _scrubbers)
        {
            sb = scrubber(sb);
        }
        var lines = sb.ToString().Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            foreach (var lineScrubber in _lineScrubbers)
            {
                lines[i] = lineScrubber(lines[i]);
            }
        }
        var final = string.Join("\n", lines);

        if (!File.Exists(_verifiedPath))
        {
            await FilePolyfill.WriteAllTextAsync(_receivedPath, final);
            throw new InvalidOperationException($"No verified file found for '{name}'.");
        }

        var approved = await FilePolyfill.ReadAllTextAsync(_verifiedPath);

        if (!string.Equals(final, approved, StringComparison.Ordinal))
        {
            await FilePolyfill.WriteAllTextAsync(_receivedPath, final);

            if (_onVerifyMismatch != null)
            {
                await _onVerifyMismatch((_receivedPath, _verifiedPath), $"Verification failed for '{name}'", async () => { });
            }
            else
            {
                throw new InvalidOperationException(
                    $"Verification failed for '{name}'.\nReceived: {_receivedPath}\nVerified: {_verifiedPath}\nUpdate the verified file if this change is intentional.");
            }
        }
    }

    public TaskAwaiter GetAwaiter() => ToTask().GetAwaiter();
}

public static class VerifyTUnit
{
    public static VerifySettingsTask Verify(object? value) => new(value);
}
