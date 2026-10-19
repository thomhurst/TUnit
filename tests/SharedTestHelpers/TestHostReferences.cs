using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;

namespace TUnit.Tests.Shared;

/// <summary>
/// Paths of the assemblies that in-memory test compilations (generator and analyzer tests) reference.
/// </summary>
/// <remarks>
/// <c>AppDomain.CurrentDomain.GetAssemblies()</c> only returns what the process has loaded so far, which depends on
/// how the test host was launched: IDE and <c>dotnet test</c> server-mode hosts load assemblies (e.g. System.Net.*)
/// that a plain run does not, so types such as <c>System.Net.HttpStatusCode</c> resolved or not and generator output
/// differed between launches. On .NET, <c>TRUSTED_PLATFORM_ASSEMBLIES</c> lists the app's whole deployment
/// (framework plus deps.json) up front. .NET Framework has no such list, so it keeps the loaded assemblies.
/// <para>
/// Neither source has a defined order (the TPA order differs between Linux and macOS hosts), and generators emit
/// referenced assemblies in reference order, so the paths are sorted to keep snapshots identical on every host.
/// </para>
/// </remarks>
internal static class TestHostReferences
{
    /// <summary>The assembly paths to reference, sorted by file name and then by path.</summary>
    public static IReadOnlyList<string> AssemblyPaths { get; } = Resolve();

    /// <summary>
    /// Metadata references to <see cref="AssemblyPaths"/>, followed by <paramref name="additionalAssemblies"/>.
    /// </summary>
    public static List<PortableExecutableReference> CreateReferences(params Assembly[] additionalAssemblies) =>
        CreateReferences(static _ => true, additionalAssemblies);

    /// <summary>
    /// Metadata references to the <see cref="AssemblyPaths"/> that <paramref name="includeHostAssembly"/> accepts,
    /// followed by <paramref name="additionalAssemblies"/>.
    /// </summary>
    public static List<PortableExecutableReference> CreateReferences(Func<string, bool> includeHostAssembly, params Assembly[] additionalAssemblies) =>
        AssemblyPaths
            .Where(includeHostAssembly)
            .Concat(additionalAssemblies.Select(assembly => assembly.Location))
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToList();

    /// <summary>
    /// Reads <c>TRUSTED_PLATFORM_ASSEMBLIES</c> on .NET, or the loaded assemblies on .NET Framework, and sorts them.
    /// </summary>
    private static string[] Resolve()
    {
        var paths = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trustedPlatformAssemblies
            ? trustedPlatformAssemblies.Split(new[] { Path.PathSeparator }, StringSplitOptions.RemoveEmptyEntries)
            : AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                .Select(assembly => assembly.Location);

        // Ordinal on purpose: reference order shows up in generator output, so a culture-aware sort, or no sort, would
        // reorder snapshots between hosts. A snapshot that reorders after a change here is this sort, not the generator.
        return paths
            .OrderBy(Path.GetFileName, StringComparer.Ordinal)
            .ThenBy(path => path, StringComparer.Ordinal)
            .ToArray();
    }
}
