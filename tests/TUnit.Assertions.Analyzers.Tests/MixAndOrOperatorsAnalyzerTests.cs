using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.MixAndOrOperatorsAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class MixAndOrOperatorsAnalyzerTests
{
    [Test]
    public async Task Flag_When_Mixing_And_With_Or()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        {|#0:await Assert.That(1).IsEqualTo(1).And.IsNotEqualTo(2).Or.IsEqualTo(3)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.MixAndOrConditionsAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task No_Error_When_Not_Mixing()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        await Assert.That(one).IsEqualTo(1).And.IsNotEqualTo(2);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Flag_When_Mixing_Across_Parenthesized_Chain()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        {|#0:await (Assert.That(1).IsEqualTo(1).And.IsNotEqualTo(2)).Or.IsEqualTo(3)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.MixAndOrConditionsAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task No_Error_When_Other_Combinator_Is_In_Nested_Assertion()
    {
        // The Or belongs to a separate assertion chain inside an argument lambda, not to the awaited chain.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        await Assert.That(await Run(async () => await Assert.That(1).IsEqualTo(1).Or.IsEqualTo(2)))
                            .IsEqualTo(1).And.IsNotEqualTo(2);
                    }

                    private static async Task<int> Run(Func<Task> action)
                    {
                        await action();
                        return 1;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Flag_Only_Nested_Assertion_When_It_Mixes()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        await Assert.That(await Run(async () => {|#0:await Assert.That(1).IsEqualTo(1).And.IsNotEqualTo(2).Or.IsEqualTo(3)|}))
                            .IsEqualTo(1);
                    }

                    private static async Task<int> Run(Func<Task> action)
                    {
                        await action();
                        return 1;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.MixAndOrConditionsAssertion)
                    .WithLocation(0)
            );
    }
}
