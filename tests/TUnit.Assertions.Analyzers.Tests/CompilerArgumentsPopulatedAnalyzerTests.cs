using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.CompilerArgumentsPopulatedAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class CompilerArgumentsPopulatedAnalyzerTests
{
    [Test]
    public async Task Expression_Argument_Is_Flagged_When_Populated()
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
                        await Assert.That(1, {|#0:"expression"|}).IsEqualTo(1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Not_Flagged_When_Not_Populated()
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
                        await Assert.That(1).IsEqualTo(1);
                    }

                }
                """
            );
    }

    [Test]
    public async Task Named_Expression_Argument_Is_Flagged()
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
                        await Assert.That(1, {|#0:expression: "expression"|}).IsEqualTo(1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Expression_Argument_Is_Flagged_In_Lambdas_Local_Functions_And_Initializers()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;

                public class Base
                {
                    public Base(object assertion) { }
                }

                public class MyClass : Base
                {
                    private readonly object _field = Assert.That(1, {|#0:"field"|});

                    public object Property { get; } = Assert.That(1, {|#1:"property"|});

                    public object ExpressionBodied => Assert.That(1, {|#2:"expression-bodied"|});

                    public MyClass() : base(Assert.That(1, {|#3:"ctor-initializer"|}))
                    {
                    }

                    public async Task MyTest()
                    {
                        Func<Task> lambda = async () => await Assert.That(1, {|#4:"lambda"|}).IsEqualTo(1);

                        await Local();

                        async Task Local() => await Assert.That(1, {|#5:"local"|}).IsEqualTo(1);

                        await Assert.That(Assert.That(1, {|#6:"nested"|}) is not null).IsTrue();
                    }
                }
                """,

                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(0),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(1),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(2),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(3),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(4),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(5),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(6)
            );
    }

    [Test]
    public async Task Caller_Argument_Parameters_Outside_TUnit_Assertions_Are_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Runtime.CompilerServices;

                public class MyClass
                {
                    public static string Describe(int value, [CallerArgumentExpression(nameof(value))] string? expression = null, [CallerMemberName] string member = "")
                        => expression + member;

                    public void MyTest()
                    {
                        _ = Describe(1, "explicit", "member");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Expression_Argument_Is_Flagged_For_TUnit_Assertions_Method_Returning_Void()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Assertions;

                public class MyClass
                {
                    public void MyTest(string? value)
                    {
                        Assert.NotNull(value, {|#0:"expression"|});
                    }
                }
                """,

                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task User_Method_Returning_TUnit_Assertion_Is_Not_Flagged()
    {
        // Only parameters of TUnit.Assertions' own methods are checked; the wrapper's forwarding
        // call into Assert.That is still flagged.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Runtime.CompilerServices;
                using System.Threading.Tasks;
                using TUnit.Assertions;
                using TUnit.Assertions.Extensions;
                using TUnit.Assertions.Sources;

                public class MyClass
                {
                    public static ValueAssertion<int> MyThat(int value, [CallerArgumentExpression(nameof(value))] string? expression = null)
                        => Assert.That(value, {|#0:expression|});

                    public async Task MyTest()
                    {
                        await MyThat(1, "explicit").IsEqualTo(1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Flagged_For_Global_And_Extern_Aliased_Assertion_Assemblies()
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
                        await Assert.That(1, {|#0:"expression"|}).IsEqualTo(1);
                        await AssertionsCopy::TUnit.Assertions.Assert.That(1, {|#1:"expression"|}).IsEqualTo(1);
                    }
                }
                """,
                AwaitAssertionAnalyzerTests.AssertionsCopySource,
                "AssertionsCopy",
                assertionsAlias: null,

                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(0),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(1)
            );
    }

    [Test]
    public async Task Not_Flagged_For_Extern_Aliased_Copy_When_Not_Populated()
    {
        await Verifier
            .VerifyAnalyzerWithAdditionalAliasedAssemblyAsync(
                """
                extern alias AssertionsCopy;

                using System.Threading.Tasks;

                public class MyClass
                {
                    public async Task MyTest()
                    {
                        await AssertionsCopy::TUnit.Assertions.Assert.That(1).IsEqualTo(1);
                    }
                }
                """,
                AwaitAssertionAnalyzerTests.AssertionsCopySource,
                "AssertionsCopy",
                assertionsAlias: null
            );
    }

    [Test]
    public async Task Constructor_Arguments_Of_Assertion_Assembly_Types_Are_Flagged_When_Populated()
    {
        // Object creations and constructor initializers are checked as well as invocations: any constructor
        // declared in an assembly that defines TUnit.Assertions.Assert is in scope, including an extern-aliased copy.
        await Verifier
            .VerifyAnalyzerWithAdditionalAliasedAssemblyAsync(
                """
                extern alias AssertionsCopy;

                using AssertionsCopy::TUnit.Assertions;

                public class DerivedCapture : ExpressionCapture
                {
                    public DerivedCapture(int value) : base(value, {|#0:"base-initializer"|})
                    {
                    }
                }

                public class MyClass
                {
                    public void MyTest()
                    {
                        _ = new ExpressionCapture(1, {|#1:"explicit"|});
                        ExpressionCapture targetTyped = new(1, {|#2:expression: "named"|}, {|#3:member: "member"|});
                    }
                }
                """,
                ConstructorCopySource,
                "AssertionsCopy",
                assertionsAlias: null,

                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(0),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(1),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(2),
                Verifier.Diagnostic(Rules.CompilerArgumentsPopulated).WithLocation(3)
            );
    }

    [Test]
    public async Task Constructor_Arguments_Of_Assertion_Assembly_Types_Are_Not_Flagged_When_Not_Populated()
    {
        await Verifier
            .VerifyAnalyzerWithAdditionalAliasedAssemblyAsync(
                """
                extern alias AssertionsCopy;

                using AssertionsCopy::TUnit.Assertions;

                public class DerivedCapture : ExpressionCapture
                {
                    public DerivedCapture(int value) : base(value)
                    {
                    }
                }

                public class MyClass
                {
                    public void MyTest()
                    {
                        _ = new ExpressionCapture(1);
                        ExpressionCapture targetTyped = new(1);
                    }
                }
                """,
                ConstructorCopySource,
                "AssertionsCopy",
                assertionsAlias: null
            );
    }

    // An assembly that defines TUnit.Assertions.Assert (so the analyzer treats it as an assertions assembly)
    // plus a type whose constructor has compiler-populated parameters.
    private const string ConstructorCopySource =
        """
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
            }

            public class ExpressionCapture
            {
                public ExpressionCapture(
                    int value,
                    [System.Runtime.CompilerServices.CallerArgumentExpression("value")] string? expression = null,
                    [System.Runtime.CompilerServices.CallerMemberName] string member = "")
                {
                }
            }
        }
        """;
}
