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
}
