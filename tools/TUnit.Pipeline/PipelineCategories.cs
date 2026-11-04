namespace TUnit.Pipeline;

/// <summary>
/// Module categories, selected with --categories or skipped with --ignore-categories.
/// </summary>
public static class PipelineCategories
{
    /// <summary>Versions, packs and stages the NuGet packages. Only Ubuntu runs these in the matrix (see dotnet.yml).</summary>
    public const string Packaging = "Packaging";

    /// <summary>
    /// Consumes the packed packages. Ubuntu runs these in the matrix, Windows in its own job, and
    /// macOS not at all (see dotnet.yml).
    /// </summary>
    public const string NuGetPackageTests = "NuGetPackageTests";

    /// <summary>
    /// The native AOT publishes: the test app with the AOT-mode engine tests, and the NugetTester
    /// against the packed packages. aot.yml runs them with Packaging.
    /// </summary>
    public const string Aot = "Aot";

    /// <summary>
    /// The Reflection-mode engine tests. aot.yml runs them on runners of their own, where they no
    /// longer wait for, or compete with, the other test modules (see dotnet.yml).
    /// </summary>
    public const string EngineTests = "EngineTests";

    /// <summary>
    /// Test suites whose result does not depend on the operating system: analyzers, code fixers,
    /// source-generator and public-API snapshots, templates, and the pure-logic assertion and mock
    /// libraries. macOS skips them on pull requests (see dotnet.yml), since Ubuntu and Windows
    /// already run them and macOS runners are the scarcest.
    /// </summary>
    public const string PlatformNeutral = "PlatformNeutral";
}
