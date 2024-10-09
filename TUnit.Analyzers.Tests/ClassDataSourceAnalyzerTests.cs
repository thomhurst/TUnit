using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ClassDataSourceAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ClassDataSourceAnalyzerTests
{
    [Test]
    public async Task Class_No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            [ClassDataSource<int>]
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
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Class_No_Error_Generic()
    {
        const string text = """
                            using System.Collections.Generic;
                            using TUnit.Core;

                            [ClassDataSource<int>]
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
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Class_Missing_Parameter_Error()
    {
        const string text = """
                            using TUnit.Core;

                            [ClassDataSource<int>]
                            public class {|#0:MyClass|}
                            {
                                public MyClass()
                                {
                                }
                            
                                [Test]
                                public void MyTest()
                                {
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.NoMatchingParameterClassDataSource).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Class_Wrong_Parameter_Type_Error()
    {
        const string text = """
                            using TUnit.Core;

                            [{|#0:ClassDataSource<int>|}]
                            public class MyClass
                            {
                                public MyClass(string value)
                                {
                                }
                            
                                [Test]
                                public void MyTest()
                                {
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.NoMatchingParameterClassDataSource).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Method_No_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                                [ClassDataSource<int>]
                                [Test]
                                public void MyTest(int value)
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Method_No_Error_Generic()
    {
        const string text = """
                            using System.Collections.Generic;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [ClassDataSource<int>]
                                [Test]
                                public void MyTest(int value)
                                {
                                }
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Derived_No_Error_Generic()
    {
        const string text = """
                            using System.Collections.Generic;
                            using TUnit.Core;

                            public class MyClass
                            {
                                [ClassDataSource<MyModel>]
                                [Test]
                                public void MyTest(BaseModel value)
                                {
                                }
                                
                                public record MyModel : BaseModel;
                                public record BaseModel;
                            }
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Constructor_Derived_No_Error_Generic()
    {
        const string text = """
                            using System.Collections.Generic;
                            using TUnit.Core;
                            
                            namespace TUnit;

                            [ClassDataSource<MyModel>]
                            public class MyClass(BaseModel value)
                            {
                                [Test]
                                public void MyTest()
                                {
                                }
                            }
                            
                            public record MyModel : BaseModel;
                            public record BaseModel;
                            """;
        
        await Verifier.VerifyAnalyzerAsync(text);
    }
    
    [Test]
    public async Task Method_Missing_Parameter_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                                [ClassDataSource<int>]
                                [Test]
                                public void {|#0:MyTest|}()
                                {
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.NoMatchingParameterClassDataSource).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
    
    [Test]
    public async Task Method_Wrong_Parameter_Type_Error()
    {
        const string text = """
                            using TUnit.Core;

                            public class MyClass
                            {
                                [{|#0:ClassDataSource<int>|}]
                                [Test]
                                public void MyTest(string value)
                                {
                                }
                            }
                            """;
        
        var expected = Verifier.Diagnostic(Rules.NoMatchingParameterClassDataSource).WithLocation(0);
        
        await Verifier.VerifyAnalyzerAsync(text, expected);
    }
}