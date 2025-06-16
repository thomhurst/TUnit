global using static TUnit.PublicAPI.VerifyTUnit;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace TUnit.PublicAPI;

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
        if (assembly != null)
        {
            var tfm = assembly.GetCustomAttributes(false)
                .OfType<System.Runtime.Versioning.TargetFrameworkAttribute>()
                .FirstOrDefault()?.FrameworkName ?? "unknown";
            var version = assembly.GetName().Version?.ToString() ?? "unknown";
            _uniqueSuffix = $"_{tfm}_{version}";
        }
        return this;
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
        var currentRuntime = RuntimeInformation.FrameworkDescription;
        var name = $"{testClassName}.{testName}.{currentRuntime}{_uniqueSuffix}";
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
        await FilePolyfill.WriteAllTextAsync(_receivedPath, final);

        if (!File.Exists(_verifiedPath))
        {
            throw new InvalidOperationException($"No verified file found for '{name}'.");
        }
        var approved = await FilePolyfill.ReadAllTextAsync(_verifiedPath);
        if (!string.Equals(final, approved, StringComparison.Ordinal))
        {
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
