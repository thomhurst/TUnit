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
}
