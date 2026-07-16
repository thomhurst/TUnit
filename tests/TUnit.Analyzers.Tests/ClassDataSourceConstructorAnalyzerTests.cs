using Verifier = TUnit.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Analyzers.ClassDataSourceConstructorAnalyzer>;

namespace TUnit.Analyzers.Tests;

public class ClassDataSourceConstructorAnalyzerTests
{
    [Test]
    public async Task No_Error_When_Type_Has_Parameterless_Constructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<MyData>]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    public MyData() { }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_Type_Has_Internal_Parameterless_Constructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<MyData>]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    internal MyData() { }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_Type_Is_Struct()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<MyData>]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public struct MyData
                {
                    public int Value { get; set; }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_Type_Is_Record_With_Parameterless_Constructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<MyData>]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public record MyData();
                """
            );
    }

    [Test]
    public async Task No_Error_When_Type_Has_Implicit_Parameterless_Constructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<MyData>]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    public string Value { get; set; } = "";
                }
                """
            );
    }

    [Test]
    public async Task Error_When_Type_Has_Only_Parameterized_Constructor()
    {
        var expected = Verifier.Diagnostic(Rules.NoAccessibleConstructor)
            .WithLocation(0)
            .WithArguments("MyData");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:ClassDataSource<MyData>|}]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    public MyData(string value) { }
                }
                """,
                expected
            );
    }

    [Test]
    public async Task Error_When_Type_Has_Private_Parameterless_Constructor()
    {
        var expected = Verifier.Diagnostic(Rules.NoAccessibleConstructor)
            .WithLocation(0)
            .WithArguments("MyData");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:ClassDataSource<MyData>|}]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    private MyData() { }
                    public MyData(string value) { }
                }
                """,
                expected
            );
    }

    [Test]
    public async Task Error_When_Record_Has_Required_Parameters()
    {
        var expected = Verifier.Diagnostic(Rules.NoAccessibleConstructor)
            .WithLocation(0)
            .WithArguments("MyData");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:ClassDataSource<MyData>|}]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public record MyData(string Value);
                """,
                expected
            );
    }

    [Test]
    public async Task Error_When_Used_On_Method_Parameter()
    {
        var expected = Verifier.Diagnostic(Rules.NoAccessibleConstructor)
            .WithLocation(0)
            .WithArguments("MyData");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:ClassDataSource<MyData>|}]
                    [Test]
                    public void MyTest(MyData data)
                    {
                    }
                }

                public class MyData
                {
                    public MyData(string value) { }
                }
                """,
                expected
            );
    }

    [Test]
    public async Task Error_When_Used_On_Class()
    {
        var expected = Verifier.Diagnostic(Rules.NoAccessibleConstructor)
            .WithLocation(0)
            .WithArguments("MyData");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                [{|#0:ClassDataSource<MyData>|}]
                public class MyClass
                {
                    public MyClass(MyData data) { }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    public MyData(string value) { }
                }
                """,
                expected
            );
    }

    [Test]
    public async Task No_Error_When_Type_Is_Abstract()
    {
        // Abstract types can't be instantiated anyway, so we don't report an error
        // The user likely intends to use a derived type
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<MyData>]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public abstract class MyData
                {
                    protected MyData(string value) { }
                }
                """
            );
    }

    [Test]
    public async Task No_Error_When_Protected_Internal_Constructor()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<MyData>]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    protected internal MyData() { }
                }
                """
            );
    }

    [Test]
    public async Task Error_When_Protected_Constructor_Only()
    {
        var expected = Verifier.Diagnostic(Rules.NoAccessibleConstructor)
            .WithLocation(0)
            .WithArguments("MyData");

        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [{|#0:ClassDataSource<MyData>|}]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    protected MyData() { }
                }
                """,
                expected
            );
    }

    [Test]
    public async Task No_Error_When_Type_Has_Both_Parameterless_And_Parameterized_Constructors()
    {
        await Verifier
            .VerifyAnalyzerAsync(
                """
                using TUnit.Core;

                public class MyClass
                {
                    [ClassDataSource<MyData>]
                    public required MyData Data { get; init; }

                    [Test]
                    public void MyTest()
                    {
                    }
                }

                public class MyData
                {
                    public MyData() { }
                    public MyData(string value) { }
                }
                """
            );
    }
}
