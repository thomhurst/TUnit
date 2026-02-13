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
}
