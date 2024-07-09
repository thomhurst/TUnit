using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ConflictingTestAttributesAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ConflictingTestAttributesAnalyzerTests
{
    [Test]
    public async Task No_Conflict1()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [Test]
                                public void MyTest(int value)
                                {
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task No_Conflict2()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [DataDrivenTest]
                                [Arguments(1)]
                                public void MyTest(int value)
                                {
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task No_Conflict3()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [DataSourceDrivenTest]
                                [MethodDataSource(nameof(MyMethod))]
                                public void MyTest(int value)
                                {
                                }

                                public static int MyMethod() => 1;
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task No_Conflict4()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [CombinativeTest]
                                public void MyTest(
                                [CombinativeValues(1, 2)] int value,
                                [CombinativeValues(true, false)] bool value2
                                )
                                {
                                }
                            
                                public static int MyMethod() => 1;
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Conflict1()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [Test]
                                [DataDrivenTest]
                                [Arguments(1)]
                                public void {|#0:MyTest|}(int value)
                                {
                                }

                            }
                            """;
            
        var expected = Verifier.Diagnostic(Rules.ConflictingTestAttributes).WithLocation(0);
            
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Valid_Category_Attribute()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [Test]
                                [Category("Blah")]
                                public void MyTest(int value)
                                {
                                }

                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Invalid_Category_Attribute()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                            
                                [Test]
                                [{|#0:System.ComponentModel.Category("Blah")|}]
                                public void MyTest(int value)
                                {
                                }

                            }
                            """;
            
        var expected = Verifier.Diagnostic(Rules.Wrong_Category_Attribute).WithLocation(0);
            
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}