namespace TUnit.Pipeline;

/// <summary>
/// Module categories, selected with --categories or skipped with --ignore-categories.
/// </summary>
public static class PipelineCategories
{
    /// <summary>Versions, packs and stages the NuGet packages.</summary>
    public const string Packaging = "Packaging";

    /// <summary>Consumes the packed packages. Windows runs these in their own job (see dotnet.yml).</summary>
    public const string NuGetPackageTests = "NuGetPackageTests";

    /// <summary>
    /// The native AOT publishes: the test app with the AOT-mode engine tests, and the NugetTester
    /// against the packed packages. aot.yml runs them with Packaging.
    /// </summary>
    public const string Aot = "Aot";
}
