using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.PreferIsTrueOrIsFalseAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class PreferIsTrueOrIsFalseAnalyzerTests
{
    // NOTE: Snippets use the explicit type argument `IsEqualTo<bool>(...)` so the
    // analyzer-test compiler (Roslyn 4.8 — bound by the testing harness) does not
    // raise CS0121 between the source-generated `IsEqualTo<TValue>` and the
    // implicit-conversion-aware `IsEqualTo<TValue, TOther>`. Roslyn 4.12+ honours
    // [OverloadResolutionPriority] and disambiguates without the explicit arg, so
    // this is purely a test-infrastructure workaround and does not affect users.
    [Test]
    public async Task IsEqualTo_True_Is_Flagged()
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
                    [Test]
                    public async Task MyTest()
                    {
                        var value = true;
                        await {|#0:Assert.That(value).IsEqualTo<bool>(true)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.PreferIsTrueOrIsFalseOverIsEqualToBool)
                    .WithLocation(0)
                    .WithArguments("IsTrue", "true")
            );
    }

    [Test]
    public async Task IsEqualTo_False_Is_Flagged()
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
                    [Test]
                    public async Task MyTest()
                    {
                        var value = false;
                        await {|#0:Assert.That(value).IsEqualTo<bool>(false)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.PreferIsTrueOrIsFalseOverIsEqualToBool)
                    .WithLocation(0)
                    .WithArguments("IsFalse", "false")
            );
    }

    [Test]
    public async Task No_Error_When_Using_IsTrue()
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
                    [Test]
                    public async Task MyTest()
                    {
                        var value = true;
                        await Assert.That(value).IsTrue();
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_Using_IsFalse()
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
                    [Test]
                    public async Task MyTest()
                    {
                        var value = false;
                        await Assert.That(value).IsFalse();
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_IsEqualTo_NonBool()
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
                    [Test]
                    public async Task MyTest()
                    {
                        await Assert.That(1).IsEqualTo(1);
                    }
                }
                """
            );
    }
}
