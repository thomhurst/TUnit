using System;
using System.IO;
using System.Reflection;

namespace TUnit.Tests.Shared;

/// <summary>
/// Shared resolution helper for the analyzer-test framework's reference-assembly mismatch.
/// </summary>
/// <remarks>
/// <para>
/// The Microsoft.CodeAnalysis.Testing 1.1.2 framework targets net9.0 reference assemblies. Loading
/// TUnit's net10.0 builds via <c>typeof(...).Assembly.Location</c> raises CS1705 (referenced
/// assembly has a higher System.Runtime version), which the verifier suppresses via
/// <c>CompilerDiagnostics = None</c>. The suppression silently breaks symbol resolution for every
/// extension method (IsEqualTo, Throws, etc.) in the test compilation — analyzers find nothing
/// to flag and tests report "expected 1 diagnostic, actual 0" with no compiler errors visible.
/// </para>
/// <para>
/// The fix is to load the netstandard2.0 builds (compatible with both Net90 ref assemblies and the
/// test runtime). Each analyzer test csproj copies the relevant netstandard2.0 build into the test
/// bin via a <c>&lt;None Include="..." Link="X.netstandard2.0.dll" CopyToOutputDirectory="..."&gt;</c>
/// item; this helper resolves the copy when present, falling back to the runtime assembly path so
/// the build doesn't hard-fail before the copy item runs.
/// </para>
/// <para>
/// Linked into multiple test projects via <c>&lt;Compile Include="..\SharedTestHelpers\..."&gt;</c>;
/// see <c>TUnit.Assertions.Should.SourceGenerator</c> for the same pattern with
/// <c>CovarianceHelper</c>/<c>EquatableArray</c>.
/// </para>
/// </remarks>
internal static class AnalyzerTestCompatibility
{
    public static string GetCompatibleDllPath(string assemblyName, Assembly fallback)
    {
        var ns20Path = Path.Combine(AppContext.BaseDirectory, $"{assemblyName}.netstandard2.0.dll");
        return File.Exists(ns20Path) ? ns20Path : fallback.Location;
    }
}
