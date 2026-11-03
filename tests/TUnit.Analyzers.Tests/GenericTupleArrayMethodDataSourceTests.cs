using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class GenericTupleArrayMethodDataSourceTests : BaseAnalyzerTests
{
    [Test]
    public async Task ExistingGenericTupleKeepsRuntimeConversions()
    {
        await Verifier.VerifyAnalyzerAsync("""
            using TUnit.Core;
            public class C
            {
                public static (int, string) Row() => (1, "2");
                [Test, MethodDataSource(nameof(Row))]
                public void Test<T>((T, int) row) { }
            }
            """);
    }

    [Test]
    [Arguments("int[]", "T[]", false)]
    [Arguments("int[][]", "T[][]", false)]
    [Arguments("int[,]", "T[,]", false)]
    [Arguments("int[]", "T[,]", true)]
    [Arguments("int", "T[]", true)]
    [Arguments("int[]", "string[]", true)]
    [Arguments("(int, int)[]", "(T, long)[]", true)]
    [Arguments("(int, int)[]", "(T, int)[]", false)]
    public async Task TupleArrayElements(string sourceElement, string targetElement, bool expectDiagnostic)
    {
        var attribute = expectDiagnostic ? "{|#0:MethodDataSource(nameof(Row))|}" : "MethodDataSource(nameof(Row))";
        var source = $$"""
            #pragma warning disable TUnit0046 // Mutable array rows are intentional in these binding tests.
            using System.Threading;
            using System.Threading.Tasks;
            using TUnit.Core;
            using TUnit.Core.Interfaces;
            public class C
            {
                public static (Fixture, CancellationToken, {{sourceElement}}) Row() => default;
                [Test, {{attribute}}]
                public void Test<T>((Fixture, CancellationToken, {{targetElement}}) row) { }
                public struct Fixture : IAsyncInitializer
                {
                    public Task InitializeAsync() => Task.CompletedTask;
                }
            }
            """;

        if (expectDiagnostic)
        {
            await Verifier.VerifyAnalyzerAsync(source,
                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData).WithLocation(0)
                    .WithArguments($"Fixture, CancellationToken, {sourceElement}", $"(Fixture, CancellationToken, {targetElement})"));
        }
        else
        {
            await Verifier.VerifyAnalyzerAsync(source);
        }
    }
}
