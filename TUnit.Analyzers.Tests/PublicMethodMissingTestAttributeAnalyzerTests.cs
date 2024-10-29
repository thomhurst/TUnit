using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.PublicMethodMissingTestAttributeAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class PublicMethodMissingTestAttributeAnalyzerTests
{
    [Test]
    public async Task Class_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                        Helper();
                    }
                                
                    private void Helper()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Class_Missing_Parameter_Error()
    {
        var expected = Verifier.Diagnostic(Rules.PublicMethodMissingTestAttribute).WithLocation(0);

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                            
                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                        Helper();
                    }
                                
                    public void {|#0:Helper|}()
                    {
                    }
                }
                """,
				expected
			);
    }

    [Test]
    public async Task Before_Hook_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                                
                    [Before(HookType.Test)]
                    public void SetUp()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task After_Hook_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                                
                    [After(HookType.Test)]
                    public void SetUp()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task IDisposable_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using TUnit.Core;

                public class MyClass : IDisposable
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                                
                    public void Dispose()
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task IAsyncDisposable_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass : IAsyncDisposable
                {
                    [Test]
                    public void MyTest()
                    {
                    }
                                
                    public ValueTask DisposeAsync()
                    {
                        return ValueTask.CompletedTask;
                    }
                }
                """
            );
    }
}