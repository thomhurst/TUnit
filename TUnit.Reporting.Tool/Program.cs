using System.Text;
using TUnit.Engine.Configuration;
using TUnit.Engine.Reporters.Aggregation;
using TUnit.Engine.Reporters.Html;

namespace TUnit.Reporting.Tool;

internal static class Program
{
    private const string Usage =
        """
        tunit-report — merge TUnit test reports from multiple test projects into one.

        Usage:
          tunit-report merge --directory <dir> [options]

        Options:
          -d, --directory <dir>    Directory scanned recursively for *.tunit-report.json
                                   sidecars (written by TUnit next to each HTML report).
          -o, --output <file>      Path for the merged HTML report.
                                   Default: <directory>/merged-report.html
          --github-summary         Also write the merged summary block to the file that
                                   $GITHUB_STEP_SUMMARY points at (or stdout when unset).
          --style <style>          Summary style: collapsible (default) or full.
          --fail-on-failures       Exit with code 1 when any merged test failed.
          -h, --help               Show this help.

        Exit codes: 0 success · 1 failures present (with --fail-on-failures) · 2 error.
        """;

    public static int Main(string[] args)
    {
        try
        {
            return Run(args);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 2;
        }
    }

    private static int Run(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help")
        {
            Console.WriteLine(Usage);
            return args.Length == 0 ? 2 : 0;
        }

        if (args[0] != "merge")
        {
            Console.Error.WriteLine($"error: unknown command '{args[0]}'.");
            Console.WriteLine(Usage);
            return 2;
        }

        string? directory = null;
        string? output = null;
        var githubSummary = false;
        var collapsible = true;
        var failOnFailures = false;

        for (var i = 1; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-d" or "--directory":
                    directory = RequireValue(args, ref i);
                    break;
                case "-o" or "--output":
                    output = RequireValue(args, ref i);
                    break;
                case "--github-summary":
                    githubSummary = true;
                    break;
                case "--style":
                    collapsible = RequireValue(args, ref i).ToLowerInvariant() switch
                    {
                        "collapsible" => true,
                        "full" => false,
                        var other => throw new ArgumentException($"unknown --style '{other}' (expected 'collapsible' or 'full')"),
                    };
                    break;
                case "--fail-on-failures":
                    failOnFailures = true;
                    break;
                case "-h" or "--help":
                    Console.WriteLine(Usage);
                    return 0;
                default:
                    Console.Error.WriteLine($"error: unknown option '{args[i]}'.");
                    return 2;
            }
        }

        if (directory is null)
        {
            Console.Error.WriteLine("error: --directory is required.");
            return 2;
        }

        directory = Path.GetFullPath(directory);
        if (!Directory.Exists(directory))
        {
            Console.Error.WriteLine($"error: directory not found: {directory}");
            return 2;
        }

        var (suites, skipped) = LoadSidecars(directory);
        if (suites.Count == 0)
        {
            Console.Error.WriteLine(
                $"error: no TUnit report sidecars (*{ReportDataJson.SidecarExtension}) found under {directory}. " +
                "Sidecars are written by TUnit next to each HTML report unless TUNIT_DISABLE_JSON_REPORT is set.");
            return 2;
        }

        if (skipped > 0)
        {
            Console.WriteLine($"warning: skipped {skipped} unreadable or incompatible sidecar file(s).");
        }

        var merged = ReportDataMerger.Merge(suites);
        output ??= Path.Combine(directory, ReportDataJson.MergedReportFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(output, HtmlReportGenerator.GenerateHtml(merged), Encoding.UTF8);

        var s = merged.Summary;
        Console.WriteLine($"Merged {suites.Count} suite(s): {s.Total} tests — {s.Passed} passed, {s.TotalFailed} failed, {s.Skipped} skipped.");
        Console.WriteLine($"Merged HTML report written to: {output}");

        if (githubSummary)
        {
            WriteGitHubSummary(suites, collapsible);
        }

        return failOnFailures && s.TotalFailed + s.Cancelled > 0 ? 1 : 0;
    }

    private static (List<ReportData> Suites, int Skipped) LoadSidecars(string directory)
    {
        var suites = new List<ReportData>();
        var skipped = 0;
        // The same suite's sidecar can appear twice in one tree (the copy next to the HTML
        // report plus the copy in a TUNIT_AGGREGATE_DIR inside the scanned directory).
        // Both are byte-identical output of the same writer, so dedupe on a content hash —
        // digests, not whole file bodies, are what stays alive across the scan.
        var seenDigests = new HashSet<string>(StringComparer.Ordinal);
        using var sha = System.Security.Cryptography.SHA256.Create();
        foreach (var file in Directory.EnumerateFiles(directory, "*" + ReportDataJson.SidecarExtension, SearchOption.AllDirectories))
        {
            var bytes = File.ReadAllBytes(file);
            if (!seenDigests.Add(Convert.ToBase64String(sha.ComputeHash(bytes))))
            {
                continue;
            }

            if (ReportDataJson.TryDeserialize(bytes) is { } data)
            {
                suites.Add(data);
            }
            else
            {
                skipped++;
            }
        }
        return (suites, skipped);
    }

    private static void WriteGitHubSummary(List<ReportData> suites, bool collapsible)
    {
        var serverUrl = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubServerUrl)
            ?? EnvironmentConstants.GitHubDefaultServerUrl;

        var content = AggregatedSummaryWriter.Render(suites, collapsible, serverUrl);

        var summaryPath = Environment.GetEnvironmentVariable(EnvironmentConstants.GitHubStepSummary);
        if (string.IsNullOrEmpty(summaryPath))
        {
            Console.WriteLine("GITHUB_STEP_SUMMARY is not set — printing the merged summary to stdout:");
            Console.WriteLine(content);
            return;
        }

        if (GitHubSummaryRegion.ReplaceOrAppend(summaryPath!, content))
        {
            Console.WriteLine("Merged summary written to the GitHub step summary.");
        }
        else
        {
            Console.Error.WriteLine($"warning: could not update the GitHub step summary at {summaryPath}.");
        }
    }

    private static string RequireValue(string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"option '{args[i]}' requires a value");
        }
        return args[++i];
    }
}
