using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MethodDataSourceMatchesConstructorAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class MethodDataSourceMatchesConstructorAnalyzerTests
{
    [Test]
    public async Task No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
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
                """
            );
    }

    [Test]
    public async Task No_Error_Enumerable()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
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
                """
            );
    }

    [Test]
    public async Task Missing_Method_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
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
                """,

                Verifier.Diagnostic(Rules.NoMethodFound)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Wrong_Return_Method_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
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
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
                    .WithArguments("int", "string")
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Wrong_IEnumerable_Return_Method_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
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
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
                    .WithArguments("int", "string")
                    .WithLocation(0)
            );
    }
}