using System.Reflection;

namespace TUnit.Pipeline;

/// <summary>
/// Narrows a run to the work whose result can differ by operating system (--os-specific-only).
/// Windows and macOS pull request runs use it, since Ubuntu already runs every suite (see dotnet.yml).
/// </summary>
public static class PipelineScope
{
    /// <summary>
    /// When true, <see cref="PipelineCategories.PlatformNeutral"/> suites run only the .NET
    /// Framework TFMs, which Windows alone can run; everywhere else they run nothing.
    /// </summary>
    public static bool OsSpecificOnly { get; set; }

    public static bool IsPlatformNeutral(Type moduleType) =>
        moduleType.GetCustomAttributes<ModuleCategoryAttribute>(inherit: true)
            .Any(attribute => attribute.Category == PipelineCategories.PlatformNeutral);

    public static bool IsNetFramework(string framework) =>
        framework.StartsWith("net4", StringComparison.Ordinal);
}
