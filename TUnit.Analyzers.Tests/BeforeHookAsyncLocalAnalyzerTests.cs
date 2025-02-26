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
                    
                    {|#0:[Before(Class)]
                    public void MyTest()
                    {
                        _asyncLocal.Value = 1;
                    }|}
                }
                """,
                Verifier
                    .Diagnostic(Rules.AsyncLocalCallFlowValues)
                    .WithLocation(0)
            );
    }
    
    [Test]
    public async Task FlowAsyncLocalValues_No_Error()
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
                    
                    {|#0:[Before(Class)]
                    public void MyTest(ClassHookContext context)
                    {
                        _asyncLocal.Value = 1;
                        context.FlowAsyncLocalValues();
                    }|}
                }
                """);
    }
}