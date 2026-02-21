using TUnit.Mocks.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mocks.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mocks.Analyzers.SealedClassMockAnalyzer>;

namespace TUnit.Mocks.Analyzers.Tests;

public class SealedClassMockAnalyzerTests
{
    private const string MockStub = """
        namespace TUnit.Mocks
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
                    {|#0:TUnit.Mocks.Mock.Of<MyService>()|};
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
                    {|#0:TUnit.Mocks.Mock.Of<MyService>(0)|};
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
                    TUnit.Mocks.Mock.Of<IMyService>();
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
                    TUnit.Mocks.Mock.Of<MyBaseService>();
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
                    TUnit.Mocks.Mock.Of<MyService>();
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
                    {|#0:TUnit.Mocks.Mock.Of<string>()|};
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
                    {|#0:TUnit.Mocks.Mock.OfPartial<MyService>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM001_CannotMockSealedType)
                .WithLocation(0)
                .WithArguments("MyService")
        );
    }
}
