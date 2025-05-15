using Verifier = TUnit.Assertions.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Assertions.Analyzers.AwaitValueTaskAssertThatAnalyzer>;

namespace TUnit.Assertions.Analyzers.Tests;

public class AwaitValueTaskAssertThatAnalyzerTests
{
    [Test]
    public async Task ValueTask_Assert_That_Is_Flagged_When_Not_Awaited()
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
                        await {|#0:Assert.That(MyValueTask())|}.IsNotNull();
                    }
                    
                    private ValueTask<int> MyValueTask()
                    {
                        return new ValueTask<int>(1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AwaitValueTaskInAssertThat)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task ValueTask_Assert_That_Func_Is_Flagged_When_Not_Awaited()
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
                        await {|#0:Assert.That(() => MyValueTask())|}.IsNotNull();
                    }
                    
                    private ValueTask<int> MyValueTask()
                    {
                        return new ValueTask<int>(1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.AwaitValueTaskInAssertThat)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task ValueTask_Assert_That_Is_Not_Flagged_When_Awaited()
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
                        await {|#0:Assert.That(await MyValueTask())|}.IsEqualTo(1);
                    }
                    
                    private ValueTask<int> MyValueTask()
                    {
                        return new ValueTask<int>(1);
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task ValueTask_Assert_That_Func_Is_Not_Flagged_When_Awaited()
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
                        await {|#0:Assert.That(async () => await MyValueTask())|}.IsEqualTo(1);
                    }
                    
                    private ValueTask<int> MyValueTask()
                    {
                        return new ValueTask<int>(1);
                    }
                }
                """
            );
    }
}