using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MethodDataSourceMatchesConstructorAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class MethodDataSourceMatchesConstructorAnalyzerTests
{
    [Test]
    public async Task No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            [MethodDataSource(nameof(MyMethod))]
                            public class MyClass
                            {
                                public MyClass(int value)
                                {
                                }
                            
                                [Test]
                                public void MyTest()
                                {
                                }

                                public static int MyMethod() => 1;
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task No_Error_Enumerable()
    {
        const string text = """
                            using System.Collections.Generic;
                            using TUnit.Core;

                            [MethodDataSource(nameof(MyMethod))]
                            public class MyClass
                            {
                                public MyClass(int value)
                                {
                                }
                            
                                [Test]
                                public void MyTest()
                                {
                                }
                            
                                public static IEnumerable<int> MyMethod()
                                {
                                    yield return 1;
                                    yield return 2;
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Missing_Method_Error()
    {
        const string text = """
                            using TUnit.Core;

                            [{|#0:MethodDataSource("MyMethod")|}]
                            public class MyClass
                            {
                                public MyClass(int value)
                                {
                                }
                            
                                [Test]
                                public void MyTest()
                                {
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.NoMethodFound).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Wrong_Return_Method_Error()
    {
        const string text = """
                            using TUnit.Core;

                            [{|#0:MethodDataSource(nameof(MyMethod))|}]
                            public class MyClass
                            {
                                public MyClass(string value)
                                {
                                }
                            
                                [Test]
                                public void MyTest()
                                {
                                }
                                
                                public static int MyMethod() => 1;
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithArguments("int", "string")
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Wrong_IEnumerable_Return_Method_Error()
    {
        const string text = """
                            using System.Collections.Generic;
                            using TUnit.Core;

                            [{|#0:MethodDataSource(nameof(MyMethod))|}]
                            public class MyClass
                            {
                                public MyClass(string value)
                                {
                                }
                            
                                [Test]
                                public void MyTest()
                                {
                                }
                                
                                public static IEnumerable<int> MyMethod()
                                {
                                    yield return 1;
                                    yield return 2;
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithArguments("int", "string")
            .WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}