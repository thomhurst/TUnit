using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Mocks.SourceGenerator.Discovery;

namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Tests for <see cref="MockNamespaceConflictDetector"/> — the helper that decides
/// whether placing a generated mock alongside its target type would collide with an
/// existing user-declared type in the same namespace.
/// </summary>
public class MockNamespaceConflictTests
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

        var result = MockNamespaceConflictDetector.HasConflict(compilation, target, hasEvents: false);

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

        var result = MockNamespaceConflictDetector.HasConflict(compilation, target, hasEvents: false);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task EventsExtensionsCollision_OnlyFlaggedWhenTypeHasEvents()
    {
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

        var withoutEvents = MockNamespaceConflictDetector.HasConflict(compilation, target, hasEvents: false);
        var withEvents = MockNamespaceConflictDetector.HasConflict(compilation, target, hasEvents: true);

        await Assert.That(withoutEvents).IsFalse();
        await Assert.That(withEvents).IsTrue();
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

        var result = MockNamespaceConflictDetector.HasConflict(compilation, target, hasEvents: false);

        await Assert.That(result).IsTrue();
    }

    private static (Compilation, INamedTypeSymbol) CompileAndGetType(string source, string fullyQualifiedName)
    {
        var tree = CSharpSyntaxTree.ParseText(source,
            CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
        var refs = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location));
        var compilation = CSharpCompilation.Create("Test", [tree], refs);
        var type = compilation.GetTypeByMetadataName(fullyQualifiedName)
            ?? throw new InvalidOperationException($"Type not found: {fullyQualifiedName}");
        return (compilation, type);
    }
}
