using NUnit.Framework;
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
}