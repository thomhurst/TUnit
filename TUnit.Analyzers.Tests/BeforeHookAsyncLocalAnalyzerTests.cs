using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.BeforeHookAsyncLocalAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class BeforeHookAsyncLocalAnalyzerTests
{
    [Test]
    public async Task Void_Raises_No_Error_Setting_AsyncLocal()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading;
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;
                            
                public class MyClass
                {
                    private static readonly AsyncLocal<int> _asyncLocal = new();
                    
                    [Before(Test)]
                    public void {|#0:MyTest|}()
                    {
                        _asyncLocal.Value = 1;
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Async_Raises_Error_Setting_AsyncLocal()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading;
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;
                            
                public class MyClass
                {
                    private static readonly AsyncLocal<int> _asyncLocal = new();
                    
                    {|#1:[Before(Test)]
                    public async Task MyTest()
                    {
                        {|#0:_asyncLocal.Value = 1|};
                        await Task.Yield();
                    }|}
                }
                """,

                Verifier
                    .Diagnostic(Rules.AsyncLocalVoidMethod)
                    .WithLocation(0)
                    .WithLocation(1)
            );
    }
    
    [Test]
    public async Task Async_Raises_Error_Setting_AsyncLocal_Nested_Method()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading;
                using System.Threading.Tasks;
                using TUnit.Core;
                using static TUnit.Core.HookType;
                            
                public class MyClass
                {
                    private static readonly AsyncLocal<int> _asyncLocal = new();
                    
                    {|#1:[Before(Test)]
                    public async Task MyTest()
                    {
                        SetAsyncLocal();
                        await Task.Yield();
                    }|}
                    
                    private void SetAsyncLocal()
                    {
                        {|#0:_asyncLocal.Value = 1|};
                    }
                }
                """,

                Verifier
                    .Diagnostic(Rules.AsyncLocalVoidMethod)
                    .WithLocation(0)
                    .WithLocation(1)
            );
    }
}