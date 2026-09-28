using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Mocks.SourceGenerator.Discovery;

namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Locks in the gate that decides whether <c>T.Mock()</c> call sites need a binding check for an
/// existing <c>*_MockStaticExtension</c>. The test-pinned Roslyn cannot parse C# 14
/// <c>extension(...)</c> blocks, so the gate is exercised directly rather than end to end.
/// </summary>
public class MockDiscoveryCacheTests : SnapshotTestBase
{
    private const string Consumer = """
        public class TestUsage
        {
        }
        """;

    [Test]
    public async Task No_Static_Extension_Anywhere_Skips_Binding_Check()
    {
        var compilation = CreateCompilation(Consumer);

        await Assert.That(MockDiscoveryCache.For(compilation).MayReferenceGeneratedStaticExtensions(compilation)).IsFalse();
    }

    [Test]
    public async Task Source_Declared_Static_Extension_In_Other_Namespace_Is_Detected()
    {
        var compilation = CreateCompilation("""
            namespace Custom.Mocking
            {
                public static class INotifier_MockStaticExtension
                {
                }
            }
            """);

        await Assert.That(MockDiscoveryCache.For(compilation).MayReferenceGeneratedStaticExtensions(compilation)).IsTrue();
    }

    [Test]
    public async Task Referenced_Static_Extension_In_Other_Namespace_Is_Detected()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib.Mocking
            {
                public interface IExternalNotifier
                {
                }

                public static class IExternalNotifier_MockStaticExtension
                {
                    // Uses a TUnit.Mocks type so the assembly references TUnit.Mocks.
                    public static global::TUnit.Mocks.Mock<IExternalNotifier>? Mock() => null;
                }
            }
            """);
        var compilation = CreateCompilation(Consumer, reference);

        await Assert.That(MockDiscoveryCache.For(compilation).MayReferenceGeneratedStaticExtensions(compilation)).IsTrue();
    }

    [Test]
    public async Task Referenced_Assembly_Without_TUnit_Mocks_Reference_Is_Not_Walked()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib
            {
                public static class Unrelated_MockStaticExtension
                {
                }
            }
            """);
        var compilation = CreateCompilation(Consumer, reference);

        await Assert.That(MockDiscoveryCache.For(compilation).MayReferenceGeneratedStaticExtensions(compilation)).IsFalse();
    }

    private static Compilation CreateCompilation(string source, MetadataReference? reference = null)
    {
        var parseOptions = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);
        IEnumerable<MetadataReference> references = reference is null
            ? GetCachedReferences()
            : GetCachedReferences().Append(reference);

        return CSharpCompilation.Create(
            "TestAssembly",
            [CSharpSyntaxTree.ParseText(source, parseOptions)],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }
}
