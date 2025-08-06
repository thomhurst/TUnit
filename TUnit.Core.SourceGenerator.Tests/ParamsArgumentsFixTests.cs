using Verifier = TUnit.Core.SourceGenerator.Tests.Verifiers.CSharpIncrementalSourceGeneratorVerifier<TUnit.Core.SourceGenerator.Generators.TestMetadataGenerator>;

namespace TUnit.Core.SourceGenerator.Tests;

public class ParamsArgumentsFixTests
{
    [Test]
    public async Task Test()
    {
        const string source = """
            using TUnit.Core;

            public class ParamsArgumentsTests
            {
                [Test]
                [Arguments(2, 2)]
                [Arguments(20, 3, Operation.Kind.A)]
                [Arguments(20, 6, Operation.Kind.Deposit, Operation.Kind.B)]
                public void GetOperations(int dayDelta, int expectedNumberOfOperation, params Operation.Kind[] kinds)
                {
                    // Test implementation
                }
            }

            public class Operation
            {
                public enum Kind
                {
                    A,
                    B,
                    Deposit
                }
            }
            """;

        await Verifier.Verify(source);
    }
}