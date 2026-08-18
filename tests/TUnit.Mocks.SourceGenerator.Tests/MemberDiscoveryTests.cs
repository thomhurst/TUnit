using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Mocks.SourceGenerator.Discovery;

namespace TUnit.Mocks.SourceGenerator.Tests;

public class MemberDiscoveryTests : SnapshotTestBase
{
    [Test]
    public void Constructor_Discovery_Observes_Cancellation()
    {
        var compilation = CreateCompilation("""
            public class CancelableClient
            {
                public CancelableClient() { }
            }
            """);
        var type = compilation.GetTypeByMetadataName("CancelableClient")!;
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
        {
            MemberDiscovery.DiscoverConstructors(
                type,
                compilation,
                requiresFactoryAccessibleParameterTypes: true,
                cancellationToken: cancellationTokenSource.Token);
        });
    }

    [Test]
    public void Transitive_Interface_Discovery_Observes_Cancellation()
    {
        var compilation = CreateCompilation("""
            public interface IRoot
            {
                IChild Child { get; }
            }

            public interface IChild { }
            """);
        var type = compilation.GetTypeByMetadataName("IRoot")!;
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        Assert.Throws<OperationCanceledException>(() =>
        {
            MockTypeDiscovery.DiscoverTransitiveInterfaceTypes(
                type,
                [],
                compilation.Assembly,
                compilation,
                cancellationTokenSource.Token);
        });
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        return CSharpCompilation.Create(
                "TestAssembly",
                [syntaxTree],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithReferences(GetCachedReferences());
    }
}
