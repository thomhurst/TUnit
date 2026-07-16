using TUnit.Mocks.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mocks.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mocks.Analyzers.DelegateMockAnalyzer>;

namespace TUnit.Mocks.Analyzers.Tests;

public class DelegateMockAnalyzerTests
{
    private const string MockStub = """
        namespace TUnit.Mocks
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
                    {|#0:TUnit.Mocks.Mock.OfDelegate<string>()|};
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
                    {|#0:TUnit.Mocks.Mock.OfDelegate<IMyService>()|};
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
                    {|#0:TUnit.Mocks.Mock.OfDelegate<MyService>()|};
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
                    TUnit.Mocks.Mock.OfDelegate<System.Func<int, string>>();
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
                    TUnit.Mocks.Mock.OfDelegate<System.Action<int>>();
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
                    TUnit.Mocks.Mock.OfDelegate<Calculator>();
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
                    {|#0:TUnit.Mocks.Mock.OfDelegate<int>(0)|};
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
                    TUnit.Mocks.Mock.Of<string>();
                }
            }
            """
        );
    }
}
