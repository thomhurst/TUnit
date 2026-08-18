using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using TUnit.Mocks.SourceGenerator.Discovery;

namespace TUnit.Mocks.SourceGenerator.Tests;

public class MemberDiscoveryTests : SnapshotTestBase
{
    [Test]
    public void Constructor_Discovery_Observes_Cancellation()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("""
            public class CancelableClient
            {
                public CancelableClient() { }
            }
            """);
        var compilation = CSharpCompilation.Create(
                "TestAssembly",
                [syntaxTree],
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .WithReferences(GetCachedReferences());
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
}
