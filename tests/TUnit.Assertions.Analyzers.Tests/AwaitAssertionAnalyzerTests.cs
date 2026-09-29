using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.AwaitAssertionAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class AwaitAssertionAnalyzerTests
{
    [Test]
    public async Task Assert_That_Is_Flagged_When_Not_Awaited()
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
                        var one = 1;
                        {|#0:Assert.That(one)|}.IsEqualTo(1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AwaitAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Assert_That_Is_Flagged_When_Not_Awaited_Within_Scope()
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
                        var one = 1;
                        using (Assert.Multiple())
                        {
                            {|#0:Assert.That(one)|}.IsEqualTo(1);
                        }
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AwaitAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Assert_That_Is_Flagged_When_Generic_Type_Parameters_And_Not_Awaited()
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
                        var one = 1;
                        {|#0:Assert.That<long>(one)|}.IsEqualTo(1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AwaitAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Assert_Multiple_Is_Flagged_When_Not_Await_Using()
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
                        var one = 1;
                        {|#0:Assert.Multiple()|};
                    }
                }
                """,

                Verifier.Diagnostic(Rules.DisposableUsingMultiple)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Assert_Multiple_Is_Not_Flagged_When_Using_With_Scope()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var list = new List<int> { 1, 2, 3 };
                            
                        using (Assert.Multiple())
                        {
                            await Assert.That(list).IsEquivalentTo(new[] { 1, 2, 3, 4, 5 });
                            await Assert.That(list).Count().EqualTo(5);
                        }
                    }
                }
                """
            );
    }

    [Test]
    public async Task Assert_Multiple_Is_Not_Flagged_When_Using_Without_Scope()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Core;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var list = new List<int> { 1, 2, 3 };
                            
                        using var _ = Assert.Multiple();
                                    
                        await Assert.That(list).IsEquivalentTo(new[] { 1, 2, 3, 4, 5 });
                        await Assert.That(list).Count().EqualTo(5);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Assert_That_Is_Flagged_When_TUnit_Assertions_Is_Only_Referenced_Via_Extern_Alias()
    {
        await Verifier
            .VerifyAnalyzerWithAliasedReferencesAsync(
                """
                extern alias TUnitAssertions;

                using System.Threading.Tasks;
                using TUnitAssertions::TUnit.Assertions;
                using TUnitAssertions::TUnit.Assertions.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        {|#0:Assert.That(one)|}.IsEqualTo(1);
                    }
                }
                """,
                "TUnitAssertions",
                ["TUnit.Assertions.netstandard2.0.dll"],

                Verifier.Diagnostic(Rules.AwaitAssertion)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Assert_That_Is_Flagged_For_Global_And_Extern_Aliased_Assertion_Assemblies()
    {
        await Verifier
            .VerifyAnalyzerWithAdditionalAliasedAssemblyAsync(
                """
                extern alias AssertionsCopy;

                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        {|#0:Assert.That(one)|}.IsEqualTo(1);
                        {|#1:AssertionsCopy::TUnit.Assertions.Assert.That(one)|}.IsEqualTo(1);
                    }
                }
                """,
                AssertionsCopySource,
                "AssertionsCopy",
                assertionsAlias: null,

                Verifier.Diagnostic(Rules.AwaitAssertion).WithLocation(0),
                Verifier.Diagnostic(Rules.AwaitAssertion).WithLocation(1)
            );
    }

    [Test]
    public async Task Assert_That_Is_Flagged_For_Two_Extern_Aliased_Assertion_Assemblies()
    {
        await Verifier
            .VerifyAnalyzerWithAdditionalAliasedAssemblyAsync(
                """
                extern alias TUnitAssertions;
                extern alias AssertionsCopy;

                using System.Threading.Tasks;
                using TUnitAssertions::TUnit.Assertions.Extensions;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        var one = 1;
                        {|#0:TUnitAssertions::TUnit.Assertions.Assert.That(one)|}.IsEqualTo(1);
                        {|#1:AssertionsCopy::TUnit.Assertions.Assert.That(one)|}.IsEqualTo(1);
                    }
                }
                """,
                AssertionsCopySource,
                "AssertionsCopy",
                assertionsAlias: "TUnitAssertions",

                Verifier.Diagnostic(Rules.AwaitAssertion).WithLocation(0),
                Verifier.Diagnostic(Rules.AwaitAssertion).WithLocation(1)
            );
    }

    // A second assembly that also defines TUnit.Assertions.Assert, standing in for another copy/version
    // of TUnit.Assertions that is referenced through an extern alias.
    internal const string AssertionsCopySource =
        """
        using System.Threading.Tasks;

        namespace System.Runtime.CompilerServices
        {
            [AttributeUsage(AttributeTargets.Parameter)]
            internal sealed class CallerArgumentExpressionAttribute : Attribute
            {
                public CallerArgumentExpressionAttribute(string parameterName) => ParameterName = parameterName;

                public string ParameterName { get; }
            }
        }

        namespace TUnit.Assertions
        {
            public static class Assert
            {
                public static CopyAssertion That(int value, [System.Runtime.CompilerServices.CallerArgumentExpression("value")] string? expression = null)
                    => new CopyAssertion();
            }

            public sealed class CopyAssertion
            {
                public Task IsEqualTo(int expected) => Task.CompletedTask;
            }
        }
        """;
}
