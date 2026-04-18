using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    TUnit.Analyzers.TimeoutCancellationTokenAnalyzer,
    TUnit.Analyzers.CodeFixers.TimeoutCancellationTokenCodeFixProvider>;

namespace TUnit.Analyzers.Tests;

public class TimeoutCancellationTokenCodeFixProviderTests
{
    [Test]
    public async Task Adds_CancellationToken_Parameter()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Core;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public async Task {|#0:MyTest|}()
                {
                    await Task.Yield();
                }
            }
            """,
            Verifier.Diagnostic(Rules.MissingTimeoutCancellationTokenAttributes).WithLocation(0),
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public async Task MyTest(CancellationToken cancellationToken)
                {
                    await Task.Yield();
                }
            }
            """,
            test => test.CodeActionEquivalenceKey = "AddCancellationToken");
    }

    [Test]
    public async Task Adds_CancellationToken_Parameter_With_ThrowIfCancellationRequested()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Core;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public async Task {|#0:MyTest|}()
                {
                    await Task.Yield();
                }
            }
            """,
            Verifier.Diagnostic(Rules.MissingTimeoutCancellationTokenAttributes).WithLocation(0),
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public async Task MyTest(CancellationToken cancellationToken)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
            }
            """,
            test => test.CodeActionEquivalenceKey = "AddCancellationTokenWithThrow");
    }

    [Test]
    public async Task Adds_CancellationToken_Parameter_As_Discard()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Core;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public async Task {|#0:MyTest|}()
                {
                    await Task.Yield();
                }
            }
            """,
            Verifier.Diagnostic(Rules.MissingTimeoutCancellationTokenAttributes).WithLocation(0),
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public async Task MyTest(CancellationToken cancellationToken)
                {
                    _ = cancellationToken;
                    await Task.Yield();
                }
            }
            """,
            test => test.CodeActionEquivalenceKey = "AddCancellationTokenAsDiscard");
    }

    [Test]
    public async Task Does_Not_Duplicate_Existing_System_Threading_Using()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public async Task {|#0:MyTest|}()
                {
                    await Task.Yield();
                }
            }
            """,
            Verifier.Diagnostic(Rules.MissingTimeoutCancellationTokenAttributes).WithLocation(0),
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public async Task MyTest(CancellationToken cancellationToken)
                {
                    await Task.Yield();
                }
            }
            """,
            test => test.CodeActionEquivalenceKey = "AddCancellationToken");
    }

    [Test]
    public async Task Appends_CancellationToken_After_Existing_Parameters()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Core;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                [Arguments(1, "hello")]
                public async Task MyTest(int value, string {|#0:text|})
                {
                    await Task.Yield();
                }
            }
            """,
            Verifier.Diagnostic(Rules.MissingTimeoutCancellationTokenAttributes).WithLocation(0),
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                [Arguments(1, "hello")]
                public async Task MyTest(int value, string text, CancellationToken cancellationToken)
                {
                    await Task.Yield();
                }
            }
            """,
            test => test.CodeActionEquivalenceKey = "AddCancellationToken");
    }

    [Test]
    public async Task Expression_Bodied_Method_Only_Offers_Bare_Parameter_Action()
    {
        // Body-modifying variants would silently no-op on an expression-bodied method,
        // so only the bare-parameter action is registered.
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Core;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public Task {|#0:MyTest|}() => Task.CompletedTask;
            }
            """,
            Verifier.Diagnostic(Rules.MissingTimeoutCancellationTokenAttributes).WithLocation(0),
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;

            public class MyClass
            {
                [Test]
                [Timeout(1000)]
                public Task MyTest(CancellationToken cancellationToken) => Task.CompletedTask;
            }
            """,
            test => test.CodeActionEquivalenceKey = "AddCancellationToken");
    }
}
