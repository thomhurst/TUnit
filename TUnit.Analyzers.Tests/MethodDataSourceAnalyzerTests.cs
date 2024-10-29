using NUnit.Framework;
using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.MethodDataSourceAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class MethodDataSourceAnalyzerTests : BaseAnalyzerTests
{
    [Test]
    public async Task DataSourceDriven_Argument_Is_Flagged_When_Does_Not_Match_Parameter_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public void MyTest(string value)
                    {
                    }

                    public static int Data()
                    {
                        return 1;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource.Id)
                    .WithLocation(0)
                    .WithArguments("int", "string")
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Matches_Parameter_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    public void MyTest(int value)
                    {
                    }
                                
                    public static int Data()
                    {
                        return 1;
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Enumerable_Inner_Type_Matches_Parameter_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    public void MyTest(int value)
                    {
                    }
                                
                    public static IEnumerable<int> Data()
                    {
                        yield return 1;
                        yield return 2;
                        yield return 3;
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_Argument_Missing()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    public void MyTest(int value)
                    {
                    }
                                
                    public static int Data()
                    {
                        return 1;
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Data_Within_Another_Class()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public static class MyData
                {
                    public static int One()
                    {
                        return 1;
                    }
                }

                public class MyClass
                {
                    [MethodDataSource(typeof(MyData), nameof(MyData.One))]
                    public void MyTest(int value)
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Timeout_CancellationToken()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Threading;
                using TUnit.Core;

                public static class MyData
                {
                    public static int One()
                    {
                        return 1;
                    }
                }

                public class MyClass
                {
                    [Timeout(30_000)]
                    [MethodDataSource(typeof(MyData), nameof(MyData.One))]
                    public void MyTest(int value, CancellationToken token)
                    {
                    }
                }
                """
            );
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Matching_Tuple(bool includeTimeoutToken)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System.Threading;
            using TUnit.Core;

            public class MyClass
            {
                public static (int, string, bool) Tuple()
                {
                    return (1, "Hello", true);
                }
                                  
                {{GetTimeoutAttribute(includeTimeoutToken)}}
                [MethodDataSource(nameof(Tuple))]
                public void MyTest(int value, string value2, bool value3{{GetTimeoutCancellationTokenParameter(includeTimeoutToken)}})
                {
                }
            }
            """
        );
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Method_Data_Source_Is_Flagged_When_Tuple_Count_Mismatch(bool includeTimeoutToken)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System.Threading;
            using TUnit.Core;

            public class MyClass
            {
                public static (int, string, bool) Tuple()
                {
                    return (1, "Hello", true);
                }
                         
                {{GetTimeoutAttribute(includeTimeoutToken)}}
                [{|#0:MethodDataSource(nameof(Tuple))|}]
                public void MyTest(int value, string value2{{GetTimeoutCancellationTokenParameter(includeTimeoutToken)}})
                {
                }
            }
            """,
        
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource.Id)
            .WithLocation(0)
                .WithArguments("int, string, bool", "int, string")
        );
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task Method_Data_Source_Is_Flagged_When_Non_Matching_Tuple(bool includeTimeoutToken)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System.Threading;
            using TUnit.Core;

            public class MyClass
            {
                public static (int, string, bool) Tuple()
                {
                    return (1, "Hello", true);
                }
                                
                {{GetTimeoutAttribute(includeTimeoutToken)}}
                [{|#0:MethodDataSource(nameof(Tuple))|}]
                public void MyTest(int value, string value2, string value3{{GetTimeoutCancellationTokenParameter(includeTimeoutToken)}})
                {
                }
            }
            """,
        
            Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource.Id)
            .WithLocation(0)
                .WithArguments("bool", "string")
        );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_Does_Not_Match_Property_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public required string MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static int Data()
                    {
                        return 1;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource.Id)
                    .WithLocation(0)
                    .WithArguments("int", "string")
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Does_Match_Property_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public required int MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static int Data()
                    {
                        return 1;
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_Does_Not_Match_Property_Type_Enumerable()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System.Collections.Generic;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public required int MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static IEnumerable<int> Data()
                    {
                        return [1];
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource.Id)
                    .WithLocation(0)
                    .WithArguments("System.Collections.Generic.IEnumerable<int>", "int")
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_Does_Not_Match_Property_Type_Tuple()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public required (string, string) MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static (string, int) Data()
                    {
                        return ("Hello", 1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource.Id)
                    .WithLocation(0)
                    .WithArguments("string, int", "(string, string)")
            );
    }
    
    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Does_Match_Property_Type_Tuple()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                [MethodDataSource(nameof(Data))]
                    public required (string, int) MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static (string, int) Data()
                    {
                        return ("Hello", 1);
                    }
                }
                """
            );
    }
    
    [Test]
    public async Task Arguments_Are_Flagged_When_Does_Not_Match_Parameter_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = [ "Hi" ])|}]
                    public void MyTest(int value)
                    {
                    }
                            
                    public static int Data(bool flag)
                    {
                        return 1;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestDataSource)
            .WithLocation(0)
                    .WithArguments("string", "bool")
            );
    }

    [Test]
    public async Task Arguments_Are_Not_Flagged_When_Does_Match_Parameter_Types()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = [ true ])|}]
                    public void MyTest(int value)
                    {
                    }
                            
                    public static int Data(bool flag)
                    {
                        return 1;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_Are_Not_Flagged_When_Argument_Is_Convertible()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = [ new[] { 1, 2 } ])]
                    public void MyTest(int value)
                    {
                    }
                            
                    public static int Data(IEnumerable<int> values)
                    {
                        return 1;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_Are_Not_Flagged_When_Argument_Is_Convertible_Generic()
    {
        await Verifier.VerifyAnalyzerAsync(
            """
            using System.Collections.Generic;
            using TUnit.Core;

            public class MyClass
            {
            [MethodDataSource(nameof(Data), Arguments = [ new[] { 1, 2 } ])]
                public void MyTest(int value)
                {
                }
                            
                public static int Data<T>(IEnumerable<T> values)
                {
                    return 1;
                }
            }
            """
        );
    }

    [Test]
    [TestCase("new int[0]")]
    [TestCase("\"hello\"")]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Matches_Parameter_Type_Generic(string argument)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = [ {{argument}} ])]
                    public void MyTest(int value)
                    {
                    }
                      
                    public static T Data<T>(T[] values)
                    {
                        return values[0];
                    }
                }
                """
            );
    }

    [Test]
    [TestCase("new int[0]")]
    [TestCase("\"hello\"")]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Enumerable_Inner_Type_Matches_Parameter_Type_Generic(string argument)
    {
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using System.Collections.Generic;
                using TUnit.Core;
                      
                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = [ {{argument}} ])]
                    public void MyTest(int value)
                    {
                    }
                      
                    public static IEnumerable<T> Data<T>(T[] values)
                    {
                        return values;
                    }
                }
                """
            );
    }
}