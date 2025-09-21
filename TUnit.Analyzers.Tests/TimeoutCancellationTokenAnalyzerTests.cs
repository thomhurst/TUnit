using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TimeoutCancellationTokenAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class TimeoutCancellationTokenAnalyzerTests
{
    [Test]
    public async Task Test_Method_Without_CancellationToken_Shows_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            using System.Threading.Tasks;
            
            public class TestClass
            {
                [Test]
                [Timeout(30_000)]
                public async Task {|#0:TestMethod|}()
                {
                    await Task.Delay(100);
                }
            }
            """,
            Verifier.Diagnostic(Rules.MissingTimeoutCancellationTokenAttributes)
                .WithLocation(0)
        );
    }

    [Test]
    public async Task Test_Method_With_CancellationToken_Shows_No_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;
            
            public class TestClass
            {
                [Test]
                [Timeout(30_000)]
                public async Task TestMethod(CancellationToken cancellationToken)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            """
        );
    }

    [Test]
    public async Task Test_Method_With_Class_Level_Timeout_Without_CancellationToken_Shows_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            using System.Threading.Tasks;
            
            [Timeout(30_000)]
            public class TestClass
            {
                [Test]
                public async Task {|#0:TestMethod|}()
                {
                    await Task.Delay(100);
                }
            }
            """,
            Verifier.Diagnostic(Rules.MissingTimeoutCancellationTokenAttributes)
                .WithLocation(0)
        );
    }

    [Test]
    public async Task Test_Method_With_Class_Level_Timeout_With_CancellationToken_Shows_No_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;
            
            [Timeout(30_000)]
            public class TestClass
            {
                [Test]
                public async Task TestMethod(CancellationToken cancellationToken)
                {
                    await Task.Delay(100, cancellationToken);
                }
            }
            """
        );
    }

    [Test]
    public async Task Non_Test_Method_With_Class_Level_Timeout_Shows_No_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System.Net.Http;
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;
            
            [Timeout(30_000)]
            public class TestClass
            {
                private static HttpClient GetHttpClient() => new HttpClient();
                
                [Test]
                public async Task TestMethod(CancellationToken cancellationToken)
                {
                    var client = GetHttpClient();
                    await client.GetStringAsync("https://google.com/", cancellationToken);
                }
            }
            """
        );
    }

    [Test]
    public async Task Non_Test_Method_Internal_With_Class_Level_Timeout_Shows_No_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;
            
            [Timeout(30_000)]
            public class TestClass
            {
                internal void HelperMethod()
                {
                    // Some helper logic
                }
                
                [Test]
                public async Task TestMethod(CancellationToken cancellationToken)
                {
                    HelperMethod();
                    await Task.Delay(100, cancellationToken);
                }
            }
            """
        );
    }

    [Test]
    public async Task Non_Test_Method_Private_With_Class_Level_Timeout_Shows_No_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            using System.Threading;
            using System.Threading.Tasks;
            
            [Timeout(30_000)]
            public class TestClass
            {
                private async Task DoSomethingAsync()
                {
                    await Task.Delay(100);
                }
                
                [Test]
                public async Task TestMethod(CancellationToken cancellationToken)
                {
                    await DoSomethingAsync();
                    await Task.Delay(100, cancellationToken);
                }
            }
            """
        );
    }

    [Test]
    public async Task Non_Test_Non_Hook_Method_With_Method_Level_Timeout_Shows_No_Error()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using TUnit.Core;
            using System.Threading.Tasks;
            
            public class TestClass
            {
                // This shouldn't happen in practice as Timeout should only be on test/hook methods,
                // but the analyzer should not crash or report errors for non-test methods
                [Timeout(30_000)]
                private async Task HelperMethodWithTimeout()
                {
                    await Task.Delay(100);
                }
                
                [Test]
                public async Task TestMethod()
                {
                    await HelperMethodWithTimeout();
                }
            }
            """
        );
    }
}