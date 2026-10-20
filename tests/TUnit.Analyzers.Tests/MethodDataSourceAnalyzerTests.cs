using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.TestDataAnalyzer>;

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
                using System;
                
                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public void MyTest(string value)
                    {
                    }

                    public static Func<int> Data()
                    {
                        return () => 1;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
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
                using System;
                
                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                                
                    public static Func<int> Data()
                    {
                        return () => 1;
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
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                                
                    public static IEnumerable<Func<int>> Data()
                    {
                        yield return () => 1;
                        yield return () => 2;
                        yield return () => 3;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_List_Inner_Type_Matches_Parameter_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                                
                    public static List<Func<int>> Data() =>
                    [
                        () => 1,
                        () => 2,
                        () => 3,
                    ];
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
                using System;
                
                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                                
                    public static Func<int> Data()
                    {
                        return () => 1;
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
                using System;
                
                public static class MyData
                {
                    public static Func<int> One()
                    {
                        return () => 1;
                    }
                }

                public class MyClass
                {
                    [MethodDataSource(typeof(MyData), nameof(MyData.One))]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Data_Within_Another_Class_GenericAttribute()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                
                public class MyData
                {
                    public static Func<int> One()
                    {
                        return () => 1;
                    }
                }

                public class MyClass
                {
                    [MethodDataSource<MyData>(nameof(MyData.One))]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_MethodNotFound_GenericAttribute()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                
                public class MyData
                {
                    public static Func<int> One()
                    {
                        return () => 1;
                    }
                }

                public class MyClass
                {
                    [{|#0:MethodDataSource<MyData>("Two")|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                }
                """,
                Verifier.Diagnostic(Rules.NoMethodFound)
                    .WithLocation(0)
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
                using System;
                
                public static class MyData
                {
                    public static Func<int> One()
                    {
                        return () => 1;
                    }
                }

                public class MyClass
                {
                    [Timeout(30_000)]
                    [MethodDataSource(typeof(MyData), nameof(MyData.One))]
                    [Test]
                    public void MyTest(int value, CancellationToken token)
                    {
                    }
                }
                """
            );
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Matching_Tuple(bool includeTimeoutToken)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System.Threading;
            using System;
            using TUnit.Core;

            public class MyClass
            {
                public static Func<(int, string, bool)> Tuple()
                {
                    return () => (1, "Hello", true);
                }
                                  
                {{GetTimeoutAttribute(includeTimeoutToken)}}
                [MethodDataSource(nameof(Tuple))]
                [Test]
                public void MyTest(int value, string value2, bool value3{{GetTimeoutCancellationTokenParameter(includeTimeoutToken)}})
                {
                }
            }
            """
        );
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Method_Data_Source_Is_Flagged_When_Tuple_Count_Mismatch(bool includeTimeoutToken)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System.Threading;
            using System;
            using TUnit.Core;

            public class MyClass
            {
                public static Func<(int, string, bool)> Tuple()
                {
                    return () => (1, "Hello", true);
                }
                         
                {{GetTimeoutAttribute(includeTimeoutToken)}}
                [{|#0:MethodDataSource(nameof(Tuple))|}]
                [Test]
                public void MyTest(int value, string value2{{GetTimeoutCancellationTokenParameter(includeTimeoutToken)}})
                {
                }
            }
            """,

            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
            .WithLocation(0)
                .WithArguments("int, string, bool", "int, string")
        );
    }

    [Test]
    [Arguments(true)]
    [Arguments(false)]
    public async Task Method_Data_Source_Is_Flagged_When_Non_Matching_Tuple(bool includeTimeoutToken)
    {
        await Verifier.VerifyAnalyzerAsync(
            $$"""
            using System.Threading;
            using System;
            using TUnit.Core;

            public class MyClass
            {
                public static Func<(int, string, bool)> Tuple()
                {
                    return () => (1, "Hello", true);
                }
                                
                {{GetTimeoutAttribute(includeTimeoutToken)}}
                [{|#0:MethodDataSource(nameof(Tuple))|}]
                [Test]
                public void MyTest(int value, string value2, string value3{{GetTimeoutCancellationTokenParameter(includeTimeoutToken)}})
                {
                }
            }
            """,

            Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
            .WithLocation(0)
                .WithArguments("int, string, bool", "int, string, string")
        );
    }

    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_Does_Not_Match_Property_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                
                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public required string MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static Func<int> Data()
                    {
                        return () => 1;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
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
                using System;
                
                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public required int MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static Func<int> Data()
                    {
                        return () => 1;
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
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public required int MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static IEnumerable<Func<int>> {|#1:Data|}()
                    {
                        return [() => 1];
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("System.Collections.Generic.IEnumerable<System.Func<int>>", "int")
            );
    }

    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_Does_Not_Match_Property_Type_Tuple()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                
                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    public required (string, string) MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static Func<(string, int)> Data()
                    {
                        return () => ("Hello", 1);
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("(string, int)", "(string, string)")
            );
    }

    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Does_Match_Property_Type_Tuple()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                
                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    public required (string, int) MyProperty { get; init; }
                                
                    [Test]
                    public void MyTest()
                    {
                    }
                            
                    public static Func<(string, int)> Data()
                    {
                        return () => ("Hello", 1);
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_Is_Not_Flagged_When_Does_Match_Field()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    private static readonly IEnumerable<Func<int>> TestData = new List<Func<int>>
                    {
                        () => 1,
                        () => 2,
                        () => 3
                    };

                    [MethodDataSource(nameof(TestData))]
                    [Test]
                    public void MyTest(int value)
                    {
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
                using System;
                
                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = [ "Hi" ])|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                            
                    public static Func<int> Data(bool flag)
                    {
                        return () => 1;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
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
                using System;
                
                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = [ true ])|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                            
                    public static Func<int> Data(bool flag)
                    {
                        return () => 1;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Null_Argument_Is_Not_Flagged_For_Reference_Type_Parameter()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object?[] { null })]
                    [Test]
                    public void MyTest(string value)
                    {
                    }

                    public static IEnumerable<string> Data(string? value)
                    {
                        yield return value ?? "null";
                    }
                }
                """
            );
    }

    [Test]
    public async Task Null_Argument_Is_Not_Flagged_For_Nullable_Value_Type_Parameter()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object?[] { null })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(int? value)
                    {
                        yield return value ?? 0;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Null_Argument_Selects_Overload_That_Accepts_Null()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    public static IEnumerable<int> NullRows(int value) { yield return value; }
                    public static IEnumerable<string> NullRows(string? value) { yield return value ?? "null"; }

                    [Test]
                    [MethodDataSource(nameof(NullRows), Arguments = new object?[] { null })]
                    public Task Null_Argument<T>(T value) => Task.CompletedTask;
                }
                """
            );
    }

    [Test]
    public async Task Null_Argument_Is_Flagged_For_Non_Nullable_Value_Type_Parameter()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = new object?[] { null })|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(int value)
                    {
                        yield return value;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("null", "int")
            );
    }

    [Test]
    public async Task Missing_Argument_Is_Flagged_For_Parameter_That_Accepts_Null()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = new object[] { 1 })|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(int value, string? label)
                    {
                        yield return value;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int", "int, string?")
            );
    }

    [Test]
    public async Task Exact_Overload_Wins_Over_Earlier_Overload_With_CancellationToken()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using System.Threading;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1 })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<string> Data(int value, CancellationToken cancellationToken)
                    {
                        yield return "token";
                    }

                    public static IEnumerable<int> Data(int value)
                    {
                        yield return value;
                    }
                }
                """
            );
    }

    [Test]
    public async Task Null_Argument_Is_Flagged_For_Method_Type_Parameter()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                #nullable enable
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = new object?[] { null })|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data<T>(T value) where T : notnull
                    {
                        yield return 1;
                    }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("null", "T")
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
                using System;
                
                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = [ new[] { 1, 2 } ])]
                    [Test]
                    public void MyTest(int value)
                    {
                    }
                            
                    public static Func<int> Data(IEnumerable<int> values)
                    {
                        return () => 1;
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
            using System;
            using TUnit.Core;

            public class MyClass
            {
            
                [MethodDataSource(nameof(Data), Arguments = [ new[] { 1, 2 } ])]
                [Test]
                public void MyTest(int value)
                {
                }
                            
                public static Func<int> Data<T>(IEnumerable<T> values)
                {
                    return () => 1;
                }
            }
            """
        );
    }

    [Test]
    public async Task MethodDataSourceDrivenWithCancellationTokenTests()
    {
        await Verifier.VerifyAnalyzerAsync("""
                                           using System;
                                           using System.Collections.Generic;
                                           using System.Threading;
                                           using TUnit.Core;
                                           using System.Diagnostics.CodeAnalysis;
                                           
                                           [UnconditionalSuppressMessage("Usage", "TUnit0046:Return a `Func<T>` rather than a `<T>`")]
                                           public class MethodDataSourceDrivenWithCancellationTokenTests
                                           {
                                               [Test]
                                               [MethodDataSource(nameof(T))]
                                               [MethodDataSource(nameof(FuncT))]
                                               [MethodDataSource(nameof(EnumerableT))]
                                               [MethodDataSource(nameof(EnumerableFuncT))]
                                               [MethodDataSource(nameof(ArrayT))]
                                               [MethodDataSource(nameof(ArrayFuncT))]
                                               public void MyTest(int value, CancellationToken cancellationToken)
                                               {
                                                   Console.WriteLine(value);
                                               }
                                               
                                               public static int T() => 1;
                                           
                                               public static Func<int> FuncT() => () => 1;
                                           
                                               public static IEnumerable<int> EnumerableT() => [1];
                                           
                                               public static IEnumerable<Func<int>> EnumerableFuncT() => [() => 1];
                                           
                                               public static int[] ArrayT() => [1];
                                           
                                               public static Func<int>[] ArrayFuncT() => [() => 1];
                                           }
                                           """);
    }

    [Test]
    public async Task Bug_1538()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Threading.Tasks;
                using System.Collections.Generic;

                public class MyClass
                {
                    [Test]
                    [MethodDataSource(nameof(T5_DeconstructTestDataSource))]
                    public async Task T5_Deconstruct_Should_ReturnAllResults(bool expectedSuccess, string? expectedError,
                        string? expectedT0, string? expectedT1, string? expectedT2, string? expectedT3, string? expectedT4, string? expectedT5)
                    {
                    }
                    
                    public static
                        IEnumerable<(bool success, string? expectedError, string? expectedT0, string? expectedT1, string? expectedT2,
                            string? expectedT3, string? expectedT4, string? expectedT5)>
                        T5_DeconstructTestDataSource()
                    {
                        yield return (true, null, "This is a success", null, null, null, null, null);
                        yield return (false, "This is a failure", null, null, null, null, null, null);
                    }
                } 
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_Is_Flagged_When_Instance_Method_And_Wrong_Attribute()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;

                [Arguments(1)]
                public class MyClass(int _)
                {
                    [{|#0:MethodDataSource(nameof(One))|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public int One()
                    {
                        return 1;
                    }
                }
                """,
                Verifier.Diagnostic(Rules.InstanceMethodSource)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Method_Data_Source_With_TestDataRow_DisplayName_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(GetData))]
                    [Test]
                    public void MyTest(string arg1, string arg2)
                    {
                    }

                    public static IEnumerable<TestDataRow<(string, string)>> GetData()
                    {
                        yield return new(("preferLocal", "AutoResolvePreferLocal"), DisplayName: "Auto-resolve as prefer local");
                        yield return new(("preferRemote", "AutoResolvePreferRemote"), DisplayName: "Auto-resolve as prefer remote");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_With_TestDataRow_Skip_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(GetData))]
                    [Test]
                    public void MyTest(string arg1, string arg2)
                    {
                    }

                    public static IEnumerable<TestDataRow<(string, string)>> GetData()
                    {
                        yield return new(("test1", "value1"), Skip: "Temporarily skipped");
                        yield return new(("test2", "value2"));
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_With_TestDataRow_Categories_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(GetData))]
                    [Test]
                    public void MyTest(string arg)
                    {
                    }

                    public static IEnumerable<TestDataRow<string>> GetData()
                    {
                        yield return new("value1", Categories: new[] { "smoke", "fast" });
                        yield return new("value2", DisplayName: "Custom name", Categories: new[] { "integration" });
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_With_TestDataRow_All_Metadata_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(GetData))]
                    [Test]
                    public void MyTest(string arg1, string arg2)
                    {
                    }

                    public static IEnumerable<TestDataRow<(string, string)>> GetData()
                    {
                        yield return new(("test1", "value1"), DisplayName: "Test case 1", Skip: null, Categories: new[] { "smoke" });
                        yield return new(("test2", "value2"), DisplayName: "Test case 2", Categories: new[] { "integration" });
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_With_Func_TestDataRow_No_Error()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(GetData))]
                    [Test]
                    public void MyTest(string arg)
                    {
                    }

                    public static IEnumerable<Func<TestDataRow<string>>> GetData()
                    {
                        yield return () => new("value1", DisplayName: "First test");
                        yield return () => new("value2", DisplayName: "Second test");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_With_Func_TestDataRow_ReferenceType_No_Warning()
    {
        // Func<TestDataRow<T>> with reference type should NOT trigger TUnit0046
        // because Func provides the isolation
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyData(string value)
                {
                    public string Value { get; } = value;
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(GetData))]
                    [Test]
                    public void MyTest(MyData data)
                    {
                    }

                    public static IEnumerable<Func<TestDataRow<MyData>>> GetData()
                    {
                        yield return () => new(new MyData("test1"), DisplayName: "First");
                        yield return () => new(new MyData("test2"), DisplayName: "Second");
                    }
                }
                """
            );
    }

    [Test]
    public async Task Method_Data_Source_With_TestDataRow_Func_Tuple_No_Error()
    {
        // TestDataRow<Func<(int,int,int)>> should unwrap the Func and spread the tuple across the
        // three parameters (issue #6161). The Func is nested *inside* TestDataRow, so this exercises
        // the opposite nesting order from Method_Data_Source_With_Func_TestDataRow_No_Error.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;
                using System.Collections.Generic;

                public class MyClass
                {
                    [MethodDataSource(nameof(GetData))]
                    [Test]
                    public void MyTest(int a, int b, int c)
                    {
                    }

                    public static IEnumerable<TestDataRow<Func<(int, int, int)>>> GetData()
                    {
                        yield return new(() => (1, 1, 2), DisplayName: "$arg1 + $arg2 = $arg3");
                        yield return new(() => (1, 2, 3), DisplayName: "$arg1 + $arg2 = $arg3");
                    }
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_Immutable_Record()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public record MyRecord(string Name, int Value);

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(MyRecord value)
                    {
                    }

                    public static MyRecord Data() => new("Hello", 1);
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_Class_With_GetOnly_Properties()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class Immutable
                {
                    public string Name { get; }
                    public Immutable(string name) => Name = name;
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(Immutable value)
                    {
                    }

                    public static Immutable Data() => new("Hello");
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_Class_With_InitOnly_Setters()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class InitOnly
                {
                    public string? Name { get; init; }
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(InitOnly value)
                    {
                    }

                    public static InitOnly Data() => new() { Name = "Hello" };
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_Delegate_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(Action value)
                    {
                    }

                    public static Action Data() => () => { };
                }
                """
            );
    }

    [Test]
    public async Task No_Warning_For_String_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(string value)
                    {
                    }

                    public static string Data() => "Hello";
                }
                """
            );
    }

    [Test]
    public async Task Warning_For_Class_With_Settable_Property()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class Mutable
                {
                    public string? Name { get; set; }
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(Mutable value)
                    {
                    }

                    public static Mutable {|#0:Data|}() => new() { Name = "Hello" };
                }
                """,

                Verifier.Diagnostic(Rules.ReturnFunc)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Warning_For_Class_With_Private_Setter()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MutablePrivate
                {
                    public string? Name { get; private set; }
                    public MutablePrivate(string name) => Name = name;
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(MutablePrivate value)
                    {
                    }

                    public static MutablePrivate {|#0:Data|}() => new("Hello");
                }
                """,

                Verifier.Diagnostic(Rules.ReturnFunc)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Warning_For_Class_With_Public_NonReadonly_Field()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MutableField
                {
                    public string? Name;
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(MutableField value)
                    {
                    }

                    public static MutableField {|#0:Data|}() => new() { Name = "Hello" };
                }
                """,

                Verifier.Diagnostic(Rules.ReturnFunc)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Warning_For_Array_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(int[] value)
                    {
                    }

                    public static int[] {|#0:Data|}() => new[] { 1, 2, 3 };
                }
                """,

                Verifier.Diagnostic(Rules.ReturnFunc)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Warning_For_Class_With_Lazy_Property()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;
                using System;

                public class HasLazy
                {
                    public Lazy<string> Name { get; } = new(() => "Hello");
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(HasLazy value)
                    {
                    }

                    public static HasLazy {|#0:Data|}() => new();
                }
                """,

                Verifier.Diagnostic(Rules.ReturnFunc)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Warning_For_Derived_Class_From_Mutable_Base()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MutableBase
                {
                    public string? Name { get; set; }
                }

                public class Derived : MutableBase
                {
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(Derived value)
                    {
                    }

                    public static Derived {|#0:Data|}() => new() { Name = "Hello" };
                }
                """,

                Verifier.Diagnostic(Rules.ReturnFunc)
                    .WithLocation(0)
            );
    }

    [Test]
    public async Task Warning_For_Interface_Type()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public interface IMyInterface
                {
                    string Name { get; }
                }

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(IMyInterface value)
                    {
                    }

                    public static IMyInterface {|#0:Data|}() => null!;
                }
                """,

                Verifier.Diagnostic(Rules.ReturnFunc)
                    .WithLocation(0)
            );
    }

    // https://github.com/thomhurst/TUnit/issues/7104: a type-parameter test parameter accepts any data type.

    [Test]
    public async Task Generic_Method_With_GenerateGenericTest_And_Tuple_Data_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [GenerateGenericTest(typeof(long), typeof(string))]
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T1, T2>(T1 first, T2 second) => Task.CompletedTask;

                    public static IEnumerable<Func<(int, string)>> Data() { yield return () => (1, "a"); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Tuple_Data_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T1, T2>(T1 first, T2 second) => Task.CompletedTask;

                    public static IEnumerable<Func<(int, string)>> Data() { yield return () => (1, "a"); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Single_Type_Parameter_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value) => Task.CompletedTask;

                    public static IEnumerable<Func<int>> Data() { yield return () => 1; }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Single_Type_Parameter_And_Plain_Enumerable_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value) => Task.CompletedTask;

                    public static IEnumerable<int> Data() { yield return 1; }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Mixed_Generic_And_Concrete_Parameters_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T first, string second) => Task.CompletedTask;

                    public static IEnumerable<Func<(int, string)>> Data() { yield return () => (1, "a"); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Satisfied_Constraint_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value) where T : struct, IComparable => Task.CompletedTask;

                    public static IEnumerable<Func<int>> Data() { yield return () => 1; }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Self_Referencing_Constraint_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value) where T : IComparable<T> => Task.CompletedTask;

                    public static IEnumerable<Func<int>> Data() { yield return () => 1; }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Mismatched_Concrete_Parameter_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T first, int second) => Task.CompletedTask;

                    public static IEnumerable<Func<(int, string)>> Data() { yield return () => (1, "a"); }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int, string", "T, int")
            );
    }

    [Test]
    public async Task Generic_Method_With_Struct_Constraint_And_Reference_Type_Data_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value) where T : struct => Task.CompletedTask;

                    public static IEnumerable<Func<string>> Data() { yield return () => "a"; }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("string", "T")
            );
    }

    [Test]
    public async Task Generic_Method_With_Class_Constraint_And_Value_Type_Data_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value) where T : class => Task.CompletedTask;

                    public static IEnumerable<Func<int>> Data() { yield return () => 1; }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int", "T")
            );
    }

    [Test]
    public async Task Generic_Method_With_Unsatisfied_Interface_Constraint_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value) where T : IDisposable => Task.CompletedTask;

                    public static IEnumerable<Func<int>> Data() { yield return () => 1; }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int", "T")
            );
    }

    [Test]
    public async Task Generic_Method_With_Constructor_Constraint_And_No_Parameterless_Constructor_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value) where T : new() => Task.CompletedTask;

                    public static IEnumerable<Func<string>> Data() { yield return () => "a"; }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("string", "T")
            );
    }

    [Test]
    public async Task Generic_Method_With_Self_Referencing_Constraint_And_Satisfying_Reference_Type_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value, int marker) where T : IComparable<T> => Task.CompletedTask;

                    public static IEnumerable<Func<(string, int)>> Data() { yield return () => ("a", 1); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Unsatisfied_Self_Referencing_Constraint_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value, int marker) where T : IComparable<T> => Task.CompletedTask;

                    public static IEnumerable<Func<(object, int)>> Data() { yield return () => (new object(), 1); }
                }
                """,

                Verifier.Diagnostic(Rules.GenericTypeArgumentInferredFromDeclaredType)
                    .WithLocation(0)
                    .WithArguments("T", "object")
            );
    }

    [Test]
    public async Task Generic_Method_With_Interface_Constraint_And_Nullable_Value_Type_Data_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value) where T : IComparable => Task.CompletedTask;

                    public static IEnumerable<Func<int?>> Data() { yield return () => 1; }
                }
                """,

                Verifier.Diagnostic(Rules.GenericTypeArgumentInferredFromDeclaredType)
                    .WithLocation(0)
                    .WithArguments("T", "int?")
            );
    }

    [Test]
    [Arguments("object", "new System.IO.MemoryStream()")]
    [Arguments("System.EventArgs", "System.EventArgs.Empty")]
    [Arguments("System.Collections.IEnumerable", "new System.IO.MemoryStream[0]")]
    public async Task Generic_Method_With_Broad_Declared_Type_Explains_Declared_Type_Inference(string declaredType, string value)
    {
        // Source-generated mode infers T from the declared type, so a run-time MemoryStream does not help (#7116).
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value) where T : IDisposable => Task.CompletedTask;

                    public static IEnumerable<Func<{{declaredType}}>> Data() { yield return () => {{value}}; }
                }
                """,

                Verifier.Diagnostic(Rules.GenericTypeArgumentInferredFromDeclaredType)
                    .WithLocation(0)
                    .WithArguments("T", declaredType.Replace("System.Collections.", "").Replace("System.", ""))
            );
    }

    [Test]
    [Arguments("int[]", "new int[0]")]
    [Arguments("string[]", "new string[0]")]
    [Arguments("int?[]", "new int?[0]")]
    public async Task Generic_Method_With_Exact_Array_Declared_Type_Keeps_Argument_Type_Message(string declaredType, string value)
    {
        // An array of a value type or a sealed type cannot hold another array type, so it is exact (#7116).
        await Verifier
            .VerifyAnalyzerAsync(
                $$"""
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value, int marker) where T : IDisposable => Task.CompletedTask;

                    public static IEnumerable<Func<({{declaredType}}, int)>> Data() { yield return () => ({{value}}, 1); }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments($"{declaredType}, int", "T, int")
            );
    }

    [Test]
    public async Task Generic_Method_With_Covariant_Array_Declared_Type_Explains_Declared_Type_Inference()
    {
        // An object[] value may be an array of a more derived element type.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value, int marker) where T : IDisposable => Task.CompletedTask;

                    public static IEnumerable<Func<(object[], int)>> Data() { yield return () => (new object[0], 1); }
                }
                """,

                Verifier.Diagnostic(Rules.GenericTypeArgumentInferredFromDeclaredType)
                    .WithLocation(0)
                    .WithArguments("T", "object[]")
            );
    }

    [Test]
    public async Task Generic_Method_With_Exact_Declared_Type_Keeps_Argument_Type_Message()
    {
        // A sealed declared type cannot hold a value of another type, so the plain mismatch message applies.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T value) where T : IDisposable => Task.CompletedTask;

                    public static IEnumerable<Func<string>> Data() { yield return () => "a"; }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("string", "T")
            );
    }

    [Test]
    public async Task Generic_Method_With_Broad_Declared_Type_And_GenerateGenericTest_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.IO;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [GenerateGenericTest(typeof(MemoryStream))]
                    [Test]
                    public Task MyTest<T>(T value) where T : IDisposable => Task.CompletedTask;

                    public static IEnumerable<Func<object>> Data() { yield return () => new MemoryStream(); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_GenerateGenericTest_And_Data_Without_A_CSharp_Conversion_Is_Not_Flagged()
    {
        // CastHelper can still convert the value at run time, for example through AotConverterRegistry.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public sealed class A { }
                public sealed class B { }

                public class MyClass
                {
                    [GenerateGenericTest(typeof(B))]
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value) => Task.CompletedTask;

                    public static IEnumerable<Func<A>> Data() { yield return () => new A(); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Repeated_Type_Parameter_And_Data_Violating_Constraint_In_One_Occurrence_Is_Not_Flagged()
    {
        // The type argument is inferred from the last occurrence (int) and the other value is converted to it.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T first, T second) where T : struct => Task.CompletedTask;

                    public static IEnumerable<Func<(string, int)>> Data() { yield return () => ("1", 2); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Repeated_Type_Parameter_And_Same_Data_Type_Violating_Constraint_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T>(T first, T second) where T : struct => Task.CompletedTask;

                    public static IEnumerable<Func<(string, string)>> Data() { yield return () => ("1", "2"); }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("string, string", "T, T")
            );
    }

    [Test]
    public async Task Generic_Method_With_GenerateGenericTest_And_Numeric_String_Data_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.IO;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [GenerateGenericTest(typeof(int))]
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value) => Task.CompletedTask;

                    public static IEnumerable<Func<string>> Data() { yield return () => "1"; }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_GenerateGenericTest_And_Data_Narrowed_At_Run_Time_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.IO;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [GenerateGenericTest(typeof(MemoryStream))]
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value) => Task.CompletedTask;

                    public static IEnumerable<Func<Stream>> Data() { yield return () => new MemoryStream(); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_GenerateGenericTest_Ignores_Data_Type_Constraints()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [GenerateGenericTest(typeof(object))]
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T value) where T : class => Task.CompletedTask;

                    public static IEnumerable<Func<int>> Data() { yield return () => 1; }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Constraint_On_Peer_Type_Parameter_That_Is_Unsatisfied_Is_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public Task MyTest<T, U>(T first, U second) where T : IComparable<U> => Task.CompletedTask;

                    public static IEnumerable<Func<(int, string)>> Data() { yield return () => (1, "a"); }
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int, string", "T, U")
            );
    }

    [Test]
    public async Task Generic_Method_With_Constraint_On_Peer_Type_Parameter_That_Is_Satisfied_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T, U>(T first, U second) where T : IComparable<U> => Task.CompletedTask;

                    public static IEnumerable<Func<(int, int)>> Data() { yield return () => (1, 2); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Constraint_On_Peer_Type_Parameter_With_Inexact_Data_Is_Not_Flagged()
    {
        // `object` data may hold a value whose run-time type satisfies the constraint, so the peer type argument
        // is not known and the constraint is not checked.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T, U>(T first, U second) where T : IComparable<U> => Task.CompletedTask;

                    public static IEnumerable<Func<(int, object)>> Data() { yield return () => (1, 2); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Method_With_Repeated_Type_Parameter_And_Different_Data_Types_Is_Not_Flagged()
    {
        // The value bound to the other occurrence is converted at run time (numeric, IConvertible and conversion
        // operators), so differing data types do not prove a mismatch.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest<T>(T first, T second) => Task.CompletedTask;

                    public static IEnumerable<Func<(int, long)>> Data() { yield return () => (1, 2L); }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Class_With_GenerateGenericTest_And_Type_Parameter_Method_Parameter_Is_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                [GenerateGenericTest(typeof(int))]
                public class MyClass<T>
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest(T value) => Task.CompletedTask;

                    public static IEnumerable<Func<int>> Data() { yield return () => 1; }
                }
                """
            );
    }

    [Test]
    public async Task Generic_Class_Type_Parameter_Constraints_Are_Not_Checked_Against_Method_Data()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                [Arguments(1)]
                public class MyClass<T>(T seed) where T : struct
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public Task MyTest(T value) => Task.CompletedTask;

                    public static IEnumerable<Func<string>> Data() { yield return () => "a"; }
                }
                """
            );
    }

    [Test]
    public async Task Arguments_That_Fit_Two_Overloads_Equally_Are_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using System.Threading.Tasks;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Tie), Arguments = new object[] { "IComparable", 1 })|}]
                    [Test]
                    public Task MyTest(string expected, string chosen) => Task.CompletedTask;

                    public static IEnumerable<(string, string)> Tie(string expected, IComparable value) { yield return (expected, "IComparable"); }
                    public static IEnumerable<(string, string)> Tie(string expected, IConvertible value) { yield return (expected, "IConvertible"); }
                }
                """,

                Verifier.Diagnostic(Rules.AmbiguousMethodDataSourceOverload)
                    .WithLocation(0)
                    .WithArguments("Tie", "MyClass.Tie(string, IComparable), MyClass.Tie(string, IConvertible)")
            );
    }

    [Test]
    public async Task Arguments_That_Fit_Overloads_In_Opposite_Directions_Are_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = new object[] { "a", "b" })|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(string first, object second) => [1];
                    public static IEnumerable<int> Data(object first, string second) => [2];
                }
                """,

                Verifier.Diagnostic(Rules.AmbiguousMethodDataSourceOverload)
                    .WithLocation(0)
                    .WithArguments("Data", "MyClass.Data(string, object), MyClass.Data(object, string)")
            );
    }

    [Test]
    public async Task Arguments_With_A_More_Specific_Overload_Are_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object[] { "a" })]
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1 })]
                    [MethodDataSource(nameof(Numbers), Arguments = new object[] { 1 })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(object value) => [1];
                    public static IEnumerable<int> Data(string value) => [2];
                    public static IEnumerable<int> Data(IComparable value) => [3];

                    public static IEnumerable<int> Numbers(long value) => [1];
                    public static IEnumerable<int> Numbers(double value) => [2];
                }
                """
            );
    }

    [Test]
    public async Task Overloads_With_Other_Parameter_Counts_Are_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1 })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(IComparable value) => [1];
                    public static IEnumerable<int> Data(IConvertible value, int other) => [2];
                }
                """
            );
    }

    [Test]
    public async Task Overridden_Data_Source_Methods_Are_Not_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class BaseClass
                {
                    public virtual IEnumerable<int> Data(int value) => [1];
                }

                public class MyClass : BaseClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1 })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public override IEnumerable<int> Data(int value) => [2];
                }
                """
            );
    }

    [Test]
    public async Task Private_Base_Overloads_Are_Not_Flagged()
    {
        // Reflection never returns private members inherited from a base type.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class BaseClass
                {
                    private static IEnumerable<int> Data(IConvertible value) => [1];
                }

                public class MyClass : BaseClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1 })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(IComparable value) => [2];
                }
                """
            );
    }

    [Test]
    public async Task Protected_Base_Overloads_Are_Flagged()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class BaseClass
                {
                    protected static IEnumerable<int> Data(IConvertible value) => [1];
                }

                public class MyClass : BaseClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = new object[] { 1 })|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(IComparable value) => [2];
                }
                """,

                Verifier.Diagnostic(Rules.AmbiguousMethodDataSourceOverload)
                    .WithLocation(0)
                    .WithArguments("Data", "MyClass.Data(IComparable), BaseClass.Data(IConvertible)")
            );
    }

    [Test]
    public async Task By_Ref_Overloads_Are_Not_Flagged()
    {
        // A by-ref parameter's runtime type (T&) never matches the argument's type.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1 })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(IComparable value) => [1];
                    public static IEnumerable<int> Data(ref IConvertible value) => [2];
                    public static IEnumerable<int> Data(in IFormattable value) => [3];
                }
                """
            );
    }

    [Test]
    public async Task Numeric_Conversions_The_Runtime_Does_Not_Widen_Are_Not_Flagged()
    {
        // The runtime binder does not widen int to decimal, so Data(double) is the only candidate.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1 })]
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1L })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data(decimal value) => [1];
                    public static IEnumerable<int> Data(double value) => [2];
                    public static IEnumerable<int> Data(nint value) => [3];
                }
                """
            );
    }

    [Test]
    public async Task Runtime_Only_Conversions_That_Tie_Are_Flagged()
    {
        // The runtime widens byte to char, and accepts an int for an int? parameter.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Chars), Arguments = new object[] { (byte)1 })|}]
                    [{|#1:MethodDataSource(nameof(Nullables), Arguments = new object[] { 1 })|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Chars(char value) => [1];
                    public static IEnumerable<int> Chars(short value) => [2];

                    public static IEnumerable<int> Nullables(int? value) => [1];
                    public static IEnumerable<int> Nullables(long value) => [2];
                }
                """,

                Verifier.Diagnostic(Rules.AmbiguousMethodDataSourceOverload)
                    .WithLocation(0)
                    .WithArguments("Chars", "MyClass.Chars(char), MyClass.Chars(short)"),
                Verifier.Diagnostic(Rules.AmbiguousMethodDataSourceOverload)
                    .WithLocation(1)
                    .WithArguments("Nullables", "MyClass.Nullables(int?), MyClass.Nullables(long)")
            );
    }

    [Test]
    public async Task Generic_Overloads_Do_Not_Hide_A_Tie()
    {
        // A parameter typed by a method type parameter never matches at runtime, so the generic overload is ignored.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data), Arguments = new object[] { 1 })|}]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data<T>(T value) => [1];
                    public static IEnumerable<int> Data(IComparable value) => [2];
                    public static IEnumerable<int> Data(IConvertible value) => [3];
                }
                """,

                Verifier.Diagnostic(Rules.AmbiguousMethodDataSourceOverload)
                    .WithLocation(0)
                    .WithArguments("Data", "MyClass.Data(IComparable), MyClass.Data(IConvertible)")
            );
    }

    [Test]
    public async Task Generic_Overload_With_Concrete_Parameters_Can_Break_A_Tie()
    {
        // The runtime selects Data<T>(int) for an int argument, because its parameter is an exact match.
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System;
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data), Arguments = new object[] { 1 })]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data<T>(int value) => [1];
                    public static IEnumerable<int> Data(IComparable value) => [2];
                    public static IEnumerable<int> Data(IConvertible value) => [3];
                }
                """
            );
    }

    [Test]
    public async Task Non_Generic_Data_Method_Is_Used_Over_A_Generic_One_Declared_First()
    {
        // Neither mode can call Data<T>() without type arguments, so the rows come from Data().
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:MethodDataSource(nameof(Data))|}]
                    [Test]
                    public void MyTest(string value)
                    {
                    }

                    public static IEnumerable<T> Data<T>() => [];
                    public static IEnumerable<int> Data() => [1];
                }
                """,

                Verifier.Diagnostic(Rules.WrongArgumentTypeTestData)
                    .WithLocation(0)
                    .WithArguments("int", "string")
            );
    }

    [Test]
    public async Task Generic_Data_Method_Without_A_Non_Generic_Sibling_Is_Still_Found()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using System.Collections.Generic;
                using TUnit.Core;

                public class MyClass
                {
                    [MethodDataSource(nameof(Data))]
                    [Test]
                    public void MyTest(int value)
                    {
                    }

                    public static IEnumerable<int> Data<T>() => [1];
                }
                """
            );
    }
}
