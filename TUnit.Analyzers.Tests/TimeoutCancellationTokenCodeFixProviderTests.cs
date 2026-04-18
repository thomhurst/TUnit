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
            using System.Threading.Tasks;
            using System.Threading;

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
            using System.Threading.Tasks;
            using System.Threading;

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
            using System.Threading.Tasks;
            using System.Threading;

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
}
