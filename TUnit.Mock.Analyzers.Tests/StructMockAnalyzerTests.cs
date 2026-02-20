using TUnit.Mock.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mock.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mock.Analyzers.StructMockAnalyzer>;

namespace TUnit.Mock.Analyzers.Tests;

public class StructMockAnalyzerTests
{
    private const string MockStub = """
        namespace TUnit.Mock
        {
            public static class Mock
            {
                public static object Of<T>() => default!;
                public static object Of<T>(int behavior) => default!;
            }
        }
        """;

    [Test]
    public async Task Struct_Reports_TM002()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public struct MyStruct { }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.Of<MyStruct>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM002_CannotMockValueType)
                .WithLocation(0)
                .WithArguments("MyStruct")
        );
    }

    [Test]
    public async Task Int_Reports_TM002()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.Of<int>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM002_CannotMockValueType)
                .WithLocation(0)
                .WithArguments("int")
        );
    }

    [Test]
    public async Task Struct_With_Behavior_Reports_TM002()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public struct MyStruct { }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.Of<MyStruct>(0)|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM002_CannotMockValueType)
                .WithLocation(0)
                .WithArguments("MyStruct")
        );
    }

    [Test]
    public async Task Interface_Does_Not_Report_TM002()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface IMyService { }

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mock.Mock.Of<IMyService>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Class_Does_Not_Report_TM002()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class MyService { }

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mock.Mock.Of<MyService>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Enum_Reports_TM002()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public enum MyEnum { A, B, C }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.Of<MyEnum>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM002_CannotMockValueType)
                .WithLocation(0)
                .WithArguments("MyEnum")
        );
    }
}
