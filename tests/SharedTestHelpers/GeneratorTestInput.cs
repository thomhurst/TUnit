using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace TUnit.Tests.Shared;

/// <summary>
/// Parse options and input checks shared by the source-generator test harnesses.
/// </summary>
/// <remarks>
/// The repository builds with <c>LangVersion=preview</c> (Directory.Build.props). Default parse options reject newer
/// syntax such as collection expressions, so a generator under test would see broken trees and the snapshots would
/// not match a real build (#7162, #7190).
/// </remarks>
internal static class GeneratorTestInput
{
    /// <summary>Parse options matching the repository's language version.</summary>
    public static CSharpParseOptions ParseOptions { get; } = new(LanguageVersion.Preview);

    /// <summary>Parses <paramref name="source"/> with <see cref="ParseOptions"/>.</summary>
    public static SyntaxTree Parse(string source, string path = "")
    {
        return CSharpSyntaxTree.ParseText(source, ParseOptions, path);
    }

    // Fixtures are not required to compile here: some reference packages or types that the harness
    // does not load. Syntax errors and the binding errors that a too-old compiler reports for valid
    // newer syntax are different, because the generator then sees a tree a real build never produces
    // and the snapshot silently records the wrong output (#7162).
    private static readonly HashSet<string> UnsupportedSyntaxDiagnosticIds =
    [
        "CS0182", // attribute argument must be constant: collection expressions in attributes on Roslyn < 4.8
        "CS0225", // params parameter must be an array: params collections on Roslyn < 4.12
        "CS8652", // feature is in preview
    ];

    /// <summary>
    /// Throws when the input has syntax errors, or binding errors that only a compiler too old for the syntax reports.
    /// </summary>
    public static void ThrowIfInputCannotBeParsed(Compilation compilation)
    {
        var errors = compilation.SyntaxTrees
            .SelectMany(tree => tree.GetDiagnostics())
            .Concat(compilation.GetDeclarationDiagnostics().Where(d => UnsupportedSyntaxDiagnosticIds.Contains(d.Id)))
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .Select(d => d.ToString())
            .Distinct()
            .ToArray();

        if (errors.Length > 0)
        {
            throw new InvalidOperationException(
                $"The test input does not parse as a real build would:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}
