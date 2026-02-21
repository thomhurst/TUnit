using TUnit.Mock.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mock.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mock.Analyzers.DelegateMockAnalyzer>;

namespace TUnit.Mock.Analyzers.Tests;

public class DelegateMockAnalyzerTests
{
    private const string MockStub = """
        namespace TUnit.Mock
        {
            public static class Mock
            {
                public static object Of<T>() => default!;
                public static object OfDelegate<T>() => default!;
                public static object OfDelegate<T>(int behavior) => default!;
            }
        }
        """;

    [Test]
    public async Task Non_Delegate_Type_Reports_TM003()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.OfDelegate<string>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM003_OfDelegateRequiresDelegateType)
                .WithLocation(0)
                .WithArguments("string")
        );
    }

    [Test]
    public async Task Interface_Reports_TM003()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface IMyService { }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.OfDelegate<IMyService>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM003_OfDelegateRequiresDelegateType)
                .WithLocation(0)
                .WithArguments("IMyService")
        );
    }

    [Test]
    public async Task Class_Reports_TM003()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class MyService { }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.OfDelegate<MyService>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM003_OfDelegateRequiresDelegateType)
                .WithLocation(0)
                .WithArguments("MyService")
        );
    }

    [Test]
    public async Task Func_Delegate_Does_Not_Report_TM003()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mock.Mock.OfDelegate<System.Func<int, string>>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Action_Delegate_Does_Not_Report_TM003()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mock.Mock.OfDelegate<System.Action<int>>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Custom_Delegate_Does_Not_Report_TM003()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public delegate int Calculator(int a, int b);

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mock.Mock.OfDelegate<Calculator>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Non_Delegate_With_Behavior_Reports_TM003()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.OfDelegate<int>(0)|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM003_OfDelegateRequiresDelegateType)
                .WithLocation(0)
                .WithArguments("int")
        );
    }

    [Test]
    public async Task Mock_Of_Does_Not_Trigger_TM003()
    {
        // Mock.Of<T>() should not trigger TM003 — that's a different method
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mock.Mock.Of<string>();
                }
            }
            """
        );
    }
}
