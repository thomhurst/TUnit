using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.PreferIsTrueOrIsFalseAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class PreferIsTrueOrIsFalseAnalyzerTests
{
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
                        await {|#0:Assert.That(value).IsEqualTo(true)|};
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
                        await {|#0:Assert.That(value).IsEqualTo(false)|};
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
