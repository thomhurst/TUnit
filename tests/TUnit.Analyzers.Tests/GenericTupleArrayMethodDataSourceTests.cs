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
    public async Task OpenClassTuplePreservesImplicitArrayCovariance()
    {
        await Verifier.VerifyAnalyzerAsync("""
            #pragma warning disable TUnit0046
            using System.Collections.Generic;
            using TUnit.Core;
            public class C<T>
            {
                public static (int, List<T>[]) Row() => default;
                [Test, MethodDataSource(nameof(Row))]
                public void Test((int, IEnumerable<T>[]) row) { }
            }
            """);
    }

    [Test]
    public async Task SeparateParametersPreserveInferredArrayCovariance()
    {
        await Verifier.VerifyAnalyzerAsync("""
            #pragma warning disable TUnit0046
            using System.Collections.Generic;
            using TUnit.Core;
            public class C
            {
                public static (int, List<int>[], int) Row() => default;
                [Test, MethodDataSource(nameof(Row))]
                public void Test<T>(T seed, IEnumerable<T>[] values, int end) { }
            }
            """);
    }

    [Test]
    [Arguments("Outer<string>.Inner<int>[]", "Outer<object>.Inner<T>[]", true)]
    [Arguments("Outer<string>.Inner<int>[]", "Outer<string>.Inner<T>[]", false)]
    [Arguments("IPair<int, string[][]>[]", "IPair<T, object[][]>[]", false)]
    [Arguments("IConsumer<int, object[][]>[]", "IConsumer<T, string[][]>[]", false)]
    [Arguments("IConsumer<int, string[][]>[]", "IConsumer<T, object[][]>[]", true)]
    [Arguments("IPair<int, int>[]", "IPair<T, object>[]", true)]
    [Arguments("int[]", "T[]", false)]
    [Arguments("int[][]", "T[][]", false)]
    [Arguments("int[,]", "T[,]", false)]
    [Arguments("int[]", "T[,]", true)]
    [Arguments("int", "T[]", true)]
    [Arguments("int[]", "string[]", true)]
    [Arguments("(int, int)[]", "(T, long)[]", true)]
    [Arguments("(int, int)[]", "(T, int)[]", false)]
    [Arguments("(int, string[])[]", "(T, object[])[]", true)]
    [Arguments("(int, string[][])[]", "(T, object[][])[]", true)]
    [Arguments("(int, string[][])[]", "(T, string[][])[]", false)]
    [Arguments("(int, string[][])", "(T, object[][])", false)]
    public async Task TupleArrayElements(string sourceElement, string targetElement, bool expectDiagnostic)
    {
        var attribute = expectDiagnostic ? "{|#0:MethodDataSource(nameof(Row))|}" : "MethodDataSource(nameof(Row))";
        var source = $$"""
            #pragma warning disable TUnit0046 // Mutable array rows are intentional in these binding tests.
            using System.Threading;
            using System.Threading.Tasks;
            using TUnit.Core;
            using TUnit.Core.Interfaces;
            public interface IPair<out A, out B> { }
            public interface IConsumer<A, in B> { }
            public class Outer<A> { public class Inner<B> { } }
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
            // Diagnostics display the nested type name without its containing construction.
            var displayedSource = sourceElement.Replace("Outer<string>.", "");
            var displayedTarget = targetElement.Replace("Outer<object>.", "").Replace("Outer<string>.", "");
            await Verifier.VerifyAnalyzerAsync(source,
                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData).WithLocation(0)
                    .WithArguments($"Fixture, CancellationToken, {displayedSource}", $"(Fixture, CancellationToken, {displayedTarget})"));
        }
        else
        {
            await Verifier.VerifyAnalyzerAsync(source);
        }
    }
}
