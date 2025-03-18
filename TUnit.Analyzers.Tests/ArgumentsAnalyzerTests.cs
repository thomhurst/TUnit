using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ArgumentsAnalyzerTests
{
    [Test]
    public async Task Test_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments(0, 1L)] // this is ok
                    [Arguments(0, 1)] // Error TUnit0001 : Attribute argument types 'int' don't match method parameter types 'long[]'
                    public void Test(int a, params long[] arr)
                    {
                    }
                }
                """
            );
    }
}