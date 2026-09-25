using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TimeoutCancellationTokenAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class BeforeHookCancellationTokenAnalyzerTests
{
    [Test]
    [Arguments("await Task.Delay(100, {|#0:context.Execution.CancellationToken|});")]
    [Arguments("await Task.Delay(100, {|#0:context.Execution.CancellationToken|}).ConfigureAwait(false);")]
    public async Task Before_Hook_Awaiting_Operation_With_Test_Token_Shows_Warning(string statement)
    {
        // Arrange
        var source = $$"""
            using TUnit.Core;
            using System.Threading.Tasks;

            public class TestClass
            {
                [Before(HookType.Test)]
                public async Task Setup(TestContext context)
                {
                    {{statement}}
                }
            }
            """;

        // Act
        var verification = Verifier.VerifyAnalyzerAsync(source,
            new DiagnosticResult("TUnit0075", DiagnosticSeverity.Warning).WithLocation(0));

        // Assert
        await verification;
    }

    [Test]
    public async Task Before_Hook_Returning_Operation_With_Test_Token_Shows_Warning()
    {
        // Arrange
        const string source = """
            using TUnit.Core;
            using System.Threading.Tasks;

            public class TestClass
            {
                [Before(HookType.Test)]
                public Task Setup(TestContext context)
                    => Task.Delay(100, {|#0:context.Execution.CancellationToken|});
            }
            """;

        // Act
        var verification = Verifier.VerifyAnalyzerAsync(source,
            new DiagnosticResult("TUnit0075", DiagnosticSeverity.Warning).WithLocation(0));

        // Assert
        await verification;
    }

    [Test]
    [Arguments("Before", "", "")]
    [Arguments("Before", "", "[Timeout(100)]")]
    [Arguments("BeforeEvery", "static ", "")]
    public async Task Before_Hook_Returning_Setup_With_Named_Test_Token_Shows_Warning(
        string attribute, string modifier, string timeout)
    {
        // Arrange
        var source = $$"""
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class TestClass
            {
                [{{attribute}}(HookType.Test)]
                {{timeout}}
                public {{modifier}}Task Setup(TestContext context, CancellationToken cancellationToken)
                {
                    return Initialize(new(), cancellationToken: {|#0:context.Execution.CancellationToken|});
                }

                private static Task Initialize(object state, CancellationToken cancellationToken)
                    => Task.CompletedTask;
            }
            """;

        // Act
        var verification = Verifier.VerifyAnalyzerAsync(source,
            new DiagnosticResult("TUnit0075", DiagnosticSeverity.Warning).WithLocation(0));

        // Assert
        await verification;
    }

    [Test]
    [Arguments("[After(HookType.Test)]", "")]
    [Arguments("[AfterEvery(HookType.Test)]", "static ")]
    [Arguments("[Test]", "")]
    [Arguments("", "")]
    public async Task Non_Setup_Method_With_Test_Token_Shows_No_Warning(string attribute, string modifier)
    {
        // Arrange
        var source = $$"""
            using TUnit.Core;
            using System.Threading.Tasks;

            public class TestClass
            {
                {{attribute}}
                public {{modifier}}Task Execute(TestContext context)
                    => Task.Delay(100, context.Execution.CancellationToken);
            }
            """;

        // Act
        var verification = Verifier.VerifyAnalyzerAsync(source);

        // Assert
        await verification;
    }

    [Test]
    [Arguments("return Task.Delay(100, cancellationToken);")]
    [Arguments("if (context.Execution.CancellationToken.IsCancellationRequested) return Task.CompletedTask; return Task.Delay(100, cancellationToken);")]
    [Arguments("return Task.Delay(100, TestContext.Current!.Execution.CancellationToken);")]
    [Arguments("var other = context; return Task.Delay(100, other.Execution.CancellationToken);")]
    [Arguments("var token = context.Execution.CancellationToken; return Task.Delay(100, token);")]
    [Arguments("_ = Task.Delay(100, context.Execution.CancellationToken); return Task.CompletedTask;")]
    [Arguments("return Capture(context.Execution.CancellationToken);")]
    [Arguments("Func<Task> setup = () => Task.Delay(100, context.Execution.CancellationToken); return setup();")]
    [Arguments("Func<Task> setup = async () => await Task.Delay(100, context.Execution.CancellationToken); return setup();")]
    [Arguments("Task SetupLater() => Task.Delay(100, context.Execution.CancellationToken); return SetupLater();")]
    public async Task Before_Hook_With_Usage_Outside_Direct_Cancellation_Argument_Shows_No_Warning(string statement)
    {
        // Arrange
        var source = $$"""
            using System;
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class TestClass
            {
                [Before(HookType.Test)]
                public Task Setup(TestContext context, CancellationToken cancellationToken)
                {
                    {{statement}}
                }

                private static Task Capture(object value) => Task.CompletedTask;
            }
            """;

        // Act
        var verification = Verifier.VerifyAnalyzerAsync(source);

        // Assert
        await verification;
    }

    [Test]
    public async Task Before_Hook_With_Unrelated_Context_Shows_No_Warning()
    {
        // Arrange
        const string source = """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class TestClass
            {
                public TestContext? OtherContext { get; set; }

                [Before(HookType.Test)]
                public async Task Setup(TestContext context, CancellationToken cancellationToken)
                {
                    if (OtherContext is { } other)
                    {
                        await Task.Delay(100, other.Execution.CancellationToken);
                    }
                }
            }
            """;

        // Act
        var verification = Verifier.VerifyAnalyzerAsync(source);

        // Assert
        await verification;
    }

    [Test]
    public async Task Before_Hook_With_Lookalike_Context_Shows_No_Warning()
    {
        // Arrange
        const string source = """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class TestClass
            {
                [Before(HookType.Test)]
                public Task Setup(Custom.TestContext context)
                    => Task.Delay(100, context.Execution.CancellationToken);
            }

            namespace Custom
            {
                public class TestContext
                {
                    public TestExecution Execution { get; } = new();
                }

                public class TestExecution
                {
                    public CancellationToken CancellationToken => CancellationToken.None;
                }
            }
            """;

        // Act
        var verification = Verifier.VerifyAnalyzerAsync(source);

        // Assert
        await verification;
    }
}
