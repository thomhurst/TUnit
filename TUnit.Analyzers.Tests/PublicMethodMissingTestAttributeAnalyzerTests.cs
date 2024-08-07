using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.PublicMethodMissingTestAttributeAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class PublicMethodMissingTestAttributeAnalyzerTests
{
    [Test]
    public async Task Class_No_Error()
    {
        const string text = """
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
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Class_Missing_Parameter_Error()
    {
        const string text = """
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
                            """;
        
        var expected = Verifier.Diagnostic(Rules.PublicMethodMissingTestAttribute).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Before_Hook_No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public void MyTest()
                                {
                                }
                                
                                [Before(HookType.EachTest)]
                                public void SetUp()
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task After_Hook_No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                                [Test]
                                public void MyTest()
                                {
                                }
                                
                                [After(HookType.EachTest)]
                                public void SetUp()
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task IDisposable_No_Error()
    {
        const string text = """
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
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task IAsyncDisposable_No_Error()
    {
        const string text = """
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
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
}