using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.AsyncVoidAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class AsyncVoidAnalyzerTests
{
    [Test]
    public async Task Async_Void_Raises_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;
                            
                public class MyClass
                {
                    [Test]
                    public async void {|#0:Test|}()
                    {
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.AsyncVoidMethod)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Async_Task_Raises_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public async Task Test()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Async_Void_Lambda_Raises_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void Test()
                    {
                        Action action = {|#0:async () =>
                        {
                            await Task.Delay(1);
                        }|};
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.AsyncVoidMethod)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Async_Task_Lambda_Raises_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void Test()
                    {
                        Func<Task> action = async () =>
                        {
                            await Task.Delay(1);
                        };
                    }
                }
                """
            );
    }

    [Test]
    public async Task Async_Void_AnonymousDelegate_Raises_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void Test()
                    {
                        Action action = {|#0:async delegate
                        {
                            await Task.Delay(1);
                        }|};
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.AsyncVoidMethod)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Async_Void_Lambda_With_Parameter_Raises_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void Test()
                    {
                        Action<int> action = {|#0:async (x) =>
                        {
                            await Task.Delay(1);
                        }|};
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.AsyncVoidMethod)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Async_Void_SimpleLambda_Raises_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void Test()
                    {
                        Action<int> action = {|#0:async x =>
                        {
                            await Task.Delay(1);
                        }|};
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.AsyncVoidMethod)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Async_Void_NonTest_Method_Raises_No_Error()
    {
        // An async void method that is not a test or hook (e.g. an event handler)
        // must not be flagged just because the project references TUnit. See #6190.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    public event EventHandler? Ticked;

                    public void Setup()
                    {
                        Ticked += OnTicked;
                    }

                    private async void OnTicked(object? sender, EventArgs e)
                    {
                        await Task.Delay(1);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Async_Void_EventHandler_Lambda_In_NonTest_Method_Raises_No_Error()
    {
        // An async void lambda subscribed to an event from a non-test method is legitimate.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    public event EventHandler? Ticked;

                    public void Setup()
                    {
                        Ticked += async (sender, e) =>
                        {
                            await Task.Delay(1);
                        };
                    }
                }
                """
            );
    }

    [Test]
    public async Task Async_Void_Hook_Raises_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [Before(HookType.Test)]
                    public async void {|#0:Setup|}()
                    {
                        await Task.Delay(1);
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.AsyncVoidMethod)
                    .WithLocation(0)
            );
    }
}
