using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Mocks.SourceGenerator.Discovery;

namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Tests for <see cref="MockNamespaceConflictDetector"/> — the helper that decides
/// whether placing a generated mock alongside its target type would collide with an
/// existing user-declared type in the same namespace.
/// </summary>
public class MockNamespaceConflictTests : SnapshotTestBase
{
    [Test]
    public async Task NoConflict_ReturnsFalse()
    {
        var source = """
            namespace MyApp
            {
                public interface IGreeter
                {
                    string Greet(string name);
                }
            }
            """;

        var (compilation, target) = CompileAndGetType(source, "MyApp.IGreeter");

        var result = MockNamespaceConflictDetector.HasConflict(compilation, target);

        await Assert.That(result).IsFalse();
    }

    [Test]
    public async Task TypeNamedFooMockExists_ReturnsTrue()
    {
        var source = """
            namespace MyApp
            {
                public interface IGreeter
                {
                    string Greet(string name);
                }

                public class IGreeterMock
                {
                }
            }
            """;

        var (compilation, target) = CompileAndGetType(source, "MyApp.IGreeter");

        var result = MockNamespaceConflictDetector.HasConflict(compilation, target);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task EventsExtensionsCollision_AlwaysFlagged()
    {
        // The detector treats every emitted-suffix collision as a conflict, regardless
        // of whether the target type actually has events. Keeping the rule symmetric
        // ensures both call sites — model construction and auto-mock factory references
        // — reach the same fallback decision without re-deriving event lists.
        var source = """
            namespace MyApp
            {
                public interface IGreeter
                {
                    string Greet(string name);
                }

                public static class IGreeter_MockEventsExtensions
                {
                }
            }
            """;

        var (compilation, target) = CompileAndGetType(source, "MyApp.IGreeter");

        var result = MockNamespaceConflictDetector.HasConflict(compilation, target);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task GlobalNamespace_ChecksGlobalTypes()
    {
        var source = """
            public interface IGreeter
            {
                string Greet(string name);
            }

            public class IGreeterMock
            {
            }
            """;

        var (compilation, target) = CompileAndGetType(source, "IGreeter");

        var result = MockNamespaceConflictDetector.HasConflict(compilation, target);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task NestedNamespace_ChecksDeepPath()
    {
        var (compilation, type) = CompileAndGetType("""
            namespace A.B.C;
            public interface IGreeter { string Greet(string name); }
            public class IGreeterMock { }
            """, "A.B.C.IGreeter");

        await Assert.That(MockNamespaceConflictDetector.HasConflict(compilation, type)).IsTrue();
    }

    [Test]
    public async Task GenericType_DetectsCollisionByBareName()
    {
        var (compilation, type) = CompileAndGetType("""
            namespace MyApp;
            public interface IGreeter<T> { T Greet(string name); }
            public class IGreeterMock { }
            """, "MyApp.IGreeter`1");

        await Assert.That(MockNamespaceConflictDetector.HasConflict(compilation, type)).IsTrue();
    }

    [Test]
    public async Task ExternalAssemblyType_WithoutCollisionInConsumer_ReturnsFalse()
    {
        // Documents the cross-assembly contract: types referenced from another assembly
        // are checked against the consumer's compilation. Absence of a colliding type in
        // the consumer's view of that namespace returns false (no conflict).
        var externalRef = CreateExternalAssemblyReference("""
            namespace ExternalLib;
            public interface IGreeter { string Greet(string name); }
            """, assemblyName: "ExtLib");

        // Consumer source declares NO ExternalLib namespace at all.
        var tree = CSharpSyntaxTree.ParseText("class C {}",
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create(
            "Consumer", [tree], GetCachedReferences().Append(externalRef));

        var type = compilation.GetTypeByMetadataName("ExternalLib.IGreeter")!;

        await Assert.That(MockNamespaceConflictDetector.HasConflict(compilation, type)).IsFalse();
    }

    private static (Compilation, INamedTypeSymbol) CompileAndGetType(string source, string fullyQualifiedName)
    {
        var tree = CSharpSyntaxTree.ParseText(source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var compilation = CSharpCompilation.Create("Test", [tree], GetCachedReferences());
        var type = compilation.GetTypeByMetadataName(fullyQualifiedName)
            ?? throw new InvalidOperationException($"Type not found: {fullyQualifiedName}");
        return (compilation, type);
    }
}
