global using static TUnit.Core.SourceGenerator.Tests.VerifyTUnit;
using System.Reflection;
using System.Runtime.CompilerServices;
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
        _uniqueSuffix = ".Net4_7";
#else
        _uniqueSuffix = $".DotNet{Environment.Version.Major}_{Environment.Version.Minor}";
#endif

        return this;
    }

    public VerifySettingsTask UniqueForTargetFrameworkAndVersion()
    {
        return UniqueForTargetFrameworkAndVersion(typeof(VerifySettingsTask).Assembly);
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

    public VerifySettingsTask ScrubFilePaths()
    {
        // Scrub Windows-style paths (e.g., C:\Users\... or D:\git\TUnit\)
        ScrubLinesWithReplace(line => System.Text.RegularExpressions.Regex.Replace(line, 
            @"[A-Za-z]:\\\\[^""\s,)]+", 
            "PATH_SCRUBBED"));
        
        // Scrub Unix-style paths (e.g., /home/user/... or /var/lib/...)
        ScrubLinesWithReplace(line => System.Text.RegularExpressions.Regex.Replace(line, 
            @"/[a-zA-Z0-9_\-./]+/[a-zA-Z0-9_\-./]+", 
            "PATH_SCRUBBED"));
        
        return this;
    }

    public async Task ToTask()
    {
        var testContext = TestContext.Current;
        string testClassName, testName;

        if (testContext != null)
        {
            testClassName = testContext.TestDetails.ClassType.Name;
            testName = testContext.TestDetails.TestName;
        }
        else
        {
            // Fallback for when TestContext is not available (e.g., during unit testing)
            var stackTrace = new System.Diagnostics.StackTrace();
            var callingMethod = stackTrace.GetFrame(1)?.GetMethod();
            testClassName = callingMethod?.DeclaringType?.Name ?? "UnknownClass";
            testName = callingMethod?.Name ?? "UnknownTest";
        }

        var name = $"{testClassName}.{testName}{_uniqueSuffix}";

        // Fallback directory path if Sourcy is not available
        string dir;
        try
        {
            dir = Sourcy.DotNet.Projects.TUnit_Core_SourceGenerator_Tests.DirectoryName!;
        }
        catch
        {
            // Use the project directory as fallback
            dir = Path.GetDirectoryName(typeof(VerifySettingsTask).Assembly.Location) ??
                  Directory.GetCurrentDirectory();
        }
        _receivedPath = Path.Combine(dir, $"{name}.received.txt");
        _verifiedPath = Path.Combine(dir, $"{name}.verified.txt");

        string serialized;
        if (_value is string str)
        {
            serialized = str;
        }
        else if (_value is string[] stringArray)
        {
            // For string arrays (like generated source files), join them with clear separators
            // instead of JSON serializing which would escape all the newlines
            // Normalize line endings in each file first
            var normalizedArray = stringArray.Select(NormalizeNewline).ToArray();
            serialized = string.Join("\n\n// ===== FILE SEPARATOR =====\n\n", normalizedArray);
        }
        else
        {
            serialized = JsonSerializer.Serialize(_value, new JsonSerializerOptions
            {
                WriteIndented = true
            });
        }
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
        
        // Always normalize line endings before any file operations
        final = NormalizeNewline(final);

        if (!File.Exists(_verifiedPath))
        {
            await FilePolyfill.WriteAllTextAsync(_receivedPath, NormalizeNewline(final));
            throw new InvalidOperationException($"No verified file found for '{name}'.");
        }

        var approved = await FilePolyfill.ReadAllTextAsync(_verifiedPath);

        if (!string.Equals(NormalizeNewline(final), NormalizeNewline(approved), StringComparison.Ordinal))
        {
            await FilePolyfill.WriteAllTextAsync(_receivedPath, NormalizeNewline(final));

            if (_onVerifyMismatch != null)
            {
                await _onVerifyMismatch((_receivedPath, _verifiedPath), $"Verification failed for '{name}'", () => Task.CompletedTask);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Verification failed for '{name}'.\nReceived: {_receivedPath}\nVerified: {_verifiedPath}\nUpdate the verified file if this change is intentional.");
            }
        }
    }

    private string NormalizeNewline(string input)
    {
        // Normalize newlines to Unix style
        return input.Replace("\r\n", "\n").Replace("\r", "\n");
    }

    public TaskAwaiter GetAwaiter() => ToTask().GetAwaiter();
}

public static class VerifyTUnit
{
    public static VerifySettingsTask Verify(object? value) => new(value);
}
