using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.PreferIsNullAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class PreferIsNullAnalyzerTests
{
    [Test]
    public async Task IsEqualTo_Null_Is_Flagged()
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
                        string? value = null;
                        await {|#0:Assert.That(value).IsEqualTo(null)|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.PreferIsNullOverIsEqualToNull)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task No_Error_When_IsEqualTo_NonNull()
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
                        var value = "hello";
                        await Assert.That(value).IsEqualTo("world");
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_Using_IsNull()
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
                        string? value = null;
                        await Assert.That(value).IsNull();
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_IsEqualTo_Int()
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
