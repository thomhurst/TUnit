using NUnit.Framework;
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
}