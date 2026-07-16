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

    [Test]
    public async Task Arguments_With_DisplayName_Property_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments("preferLocal", "AutoResolvePreferLocal", DisplayName = "Auto-resolve as prefer local (branch)")]
                    [Arguments("preferRemote", "AutoResolvePreferRemote", DisplayName = "Auto-resolve as prefer remote (org)")]
                    public void MergeAutoResolveIsCorrect(string conflictsArg, string expectedMetadataDirectoryName)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_With_Skip_Property_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments("value1", "value2", Skip = "Skipped for testing")]
                    public void MyTest(string arg1, string arg2)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_With_Categories_Property_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments("value1", "value2", Categories = new[] { "smoke", "integration" })]
                    public void MyTest(string arg1, string arg2)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_With_Multiple_Named_Properties_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments("value1", "value2", DisplayName = "Custom name", Categories = new[] { "smoke" })]
                    public void MyTest(string arg1, string arg2)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Arguments_With_DisplayName_Property_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    [Arguments<string>("value", DisplayName = "Test with string")]
                    public void MyTest(string arg)
                    {
                    }
                }
                """
            );
    }
}
