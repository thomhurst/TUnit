using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class NestedTypeDiagnosticDisplayTests : BaseAnalyzerTests
{
    [Test]
    [Arguments("Outer<string>.D", "Outer<object>.D")]
    [Arguments("Outer<string>.Nested", "Outer<object>.Nested")]
    [Arguments("Outer<string>.Middle<int>.D", "Outer<object>.Middle<long>.D")]
    [Arguments("Outer<string>.Middle<int>.Nested", "Outer<object>.Middle<long>.Nested")]
    [Arguments("IConsumer<Outer<string>.D, Outer<object>.D>[]", "IConsumer<Outer<string>.D, Outer<string>.D>[]")]
    [Arguments("IConsumer<Outer<string>.Nested, Outer<object>.Nested>[]", "IConsumer<Outer<string>.Nested, Outer<string>.Nested>[]")]
    [Arguments("Outer<string>.Middle<int>.D[]", "Outer<object>.Middle<long>.D[]")]
    public async Task MethodDataSourcePreservesContainingTypes(string sourceType, string targetType)
    {
        var source = $$"""
            #pragma warning disable TUnit0046
            using TUnit.Core;
            public class Outer<A>
            {
                public delegate void D();
                public class Nested { }
                public class Middle<B>
                {
                    public delegate void D();
                    public class Nested { }
                }
            }
            public interface IConsumer<in A, in B> { }
            public class Tests
            {
                public static (int, {{sourceType}}) Row() => default;
                [Test, {|#0:MethodDataSource(nameof(Row))|}]
                public void Test<T>((int, {{targetType}}) row) { }
            }
            """;

        await Verifier.VerifyAnalyzerAsync(source,
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData).WithLocation(0)
                .WithArguments($"int, {sourceType}", $"(int, {targetType})"));
    }
}
