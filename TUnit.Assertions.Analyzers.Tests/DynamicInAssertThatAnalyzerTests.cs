using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.DynamicInAssertThatAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class DynamicInAssertThatAnalyzerTests
{
    [Test]
    public async Task Assert_That_Is_Flagged_When_Using_Dynamic()
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
                        dynamic one = 1;
                        await {|#0:Assert.That(one)|}.IsEqualTo(1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.DynamicValueInAssertThat)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task Assert_That_Is_Flagged_When_Using_Nullable_Dynamic()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        dynamic? one = null;
                        await {|#0:Assert.That(one)|}.IsNull();
                    }
                }
                """,

                Verifier.Diagnostic(Rules.DynamicValueInAssertThat)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task No_Error_When_Using_Casted_To_Object()
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
                        dynamic one = 1;
                        await Assert.That((object)one).IsEqualTo(1);
                    }

                }
                """
            );
    }
}