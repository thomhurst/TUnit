using TUnit.Mock.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mock.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mock.Analyzers.SealedClassMockAnalyzer>;

namespace TUnit.Mock.Analyzers.Tests;

public class SealedClassMockAnalyzerTests
{
    private const string MockStub = """
        namespace TUnit.Mock
        {
            public static class Mock
            {
                public static object Of<T>() => default!;
                public static object Of<T>(int behavior) => default!;
                public static object OfPartial<T>(params object[] args) => default!;
                public static object OfPartial<T>(int behavior, params object[] args) => default!;
            }
        }
        """;

    [Test]
    public async Task Sealed_Class_Reports_TM001()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public sealed class MyService { }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.Of<MyService>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM001_CannotMockSealedType)
                .WithLocation(0)
                .WithArguments("MyService")
        );
    }

    [Test]
    public async Task Sealed_Class_With_Behavior_Reports_TM001()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public sealed class MyService { }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.Of<MyService>(0)|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM001_CannotMockSealedType)
                .WithLocation(0)
                .WithArguments("MyService")
        );
    }

    [Test]
    public async Task Interface_Does_Not_Report_TM001()
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
    public async Task Abstract_Class_Does_Not_Report_TM001()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public abstract class MyBaseService { }

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mock.Mock.Of<MyBaseService>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Non_Sealed_Class_Does_Not_Report_TM001()
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
    public async Task String_Reports_TM001()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.Of<string>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM001_CannotMockSealedType)
                .WithLocation(0)
                .WithArguments("string")
        );
    }

    [Test]
    public async Task Sealed_Class_Via_OfPartial_Reports_TM001()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public sealed class MyService { }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mock.Mock.OfPartial<MyService>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM001_CannotMockSealedType)
                .WithLocation(0)
                .WithArguments("MyService")
        );
    }
}
