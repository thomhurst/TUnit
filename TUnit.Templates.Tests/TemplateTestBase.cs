﻿using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TUnit.Templates.Tests;

/// <summary>
/// Base class for template tests. Provides:
/// - an instance of <see cref="ILoggerFactory"/> so test failures are captured by the test harness (and cleans up after the test)
/// - an instance of <see cref="TemplateVerifierOptions"/> that points to the template to install
/// </summary>
/// <remarks>
/// Uses the <see cref="VerificationEngine"/>.
/// See https://github.com/dotnet/templating/blob/main/docs/authoring-tools/Templates-Testing-Tooling.md
/// for usage information.
/// </remarks>
public abstract partial class TemplateTestBase : IDisposable
{
    private bool _disposed;
    private readonly ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

    protected abstract string TemplateShortName { get; set; }

    protected VerificationEngine Engine => new(_loggerFactory);

    protected TemplateVerifierOptions Options =>
        new TemplateVerifierOptions(TemplateShortName)
        {
            TemplatePath = Path.Combine(TestContext.OutputDirectory!, "content", TemplateShortName),
        }.WithCustomScrubbers(ScrubbersDefinition.Empty.AddScrubber(sb =>
        {
            var original = sb.ToString();
            var matches = VersionRegex().Matches(original);
            
            foreach (Match match in matches.Where(m => m.Success))
            {
                var line = match.Groups[0].Value.Replace(match.Groups[1].Value, "1.0.0");
                sb.Replace(match.Value, line);
            }
        }, "csproj"));

    protected void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _loggerFactory.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    [GeneratedRegex("""
                    Version="([^"]*)"
                    """)]
    private static partial Regex VersionRegex();
}