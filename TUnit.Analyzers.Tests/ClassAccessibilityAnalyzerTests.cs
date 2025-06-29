using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ClassAccessibilityAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ClassAccessibilityAnalyzerTests
{
    [Test]
    public async Task Inner_Internal_Class_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public static class Outer
                {
                    static class {|#0:Inner|}
                    {
                        [BeforeEvery(HookType.TestDiscovery)]
                        public static void BeforeDiscovery()
                        {
                        }
                    }
                }
                """,
                Verifier.Diagnostic(Rules.TypeMustBePublic)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Public_No_Errors()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            
            public static class Outer
            {
                public static class Inner
                {
                    [BeforeEvery(HookType.TestDiscovery)]
                    public static void BeforeDiscovery()
                    {
                    }
                }
            
                [BeforeEvery(HookType.TestDiscovery)]
                public static void BeforeDiscovery()
                {
                }
            }
            """);
    }
}
