using TUnit.Mocks.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mocks.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mocks.Analyzers.InaccessibleConstructorMockAnalyzer>;

namespace TUnit.Mocks.Analyzers.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6493
// A class whose constructors are all inaccessible cannot be subclassed, so the generated mock impl
// used to fail with a bare CS1729 inside generated code. Generation is now skipped and this
// analyzer reports TM006 at the call site instead.
public class InaccessibleConstructorMockAnalyzerTests
{
    private const string MockStub = """
        namespace TUnit.Mocks
        {
            public static class Mock
            {
                public static object Of<T>() => default!;
                public static object Of<T>(int behavior) => default!;
                public static object Wrap<T>(T instance) => default!;
            }
        }
        """;

    [Test]
    public async Task Private_Only_Constructor_Reports_TM006()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class RuntimeProperties
            {
                private RuntimeProperties(string name) { }
            }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<RuntimeProperties>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor)
                .WithLocation(0)
                .WithArguments("RuntimeProperties")
        );
    }

    [Test]
    public async Task Generated_Static_Mock_Entry_Point_Reports_TM006()
    {
        // The generator emits `Mock()` as a C# 14 static extension member on the target type,
        // inside namespace TUnit.Mocks. The Roslyn version behind the analyzer test harness
        // predates extension blocks, so this stands in the equivalent binding shape the analyzer
        // actually matches on: `T.Mock()` resolving to a static method named Mock whose containing
        // namespace is TUnit.Mocks.
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            namespace TUnit.Mocks
            {
                public class RuntimeProperties
                {
                    private RuntimeProperties(string name) { }

                    public static object Mock() => default!;
                }
            }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.RuntimeProperties.Mock()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor)
                .WithLocation(0)
                .WithArguments("RuntimeProperties")
        );
    }

    [Test]
    public async Task Wrap_Of_Type_Without_Accessible_Constructor_Reports_TM006()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class RuntimeProperties
            {
                private RuntimeProperties(string name) { }
            }

            public class TestClass
            {
                public void Test(RuntimeProperties instance)
                {
                    {|#0:TUnit.Mocks.Mock.Wrap(instance)|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor)
                .WithLocation(0)
                .WithArguments("RuntimeProperties")
        );
    }

    [Test]
    public async Task Public_Constructor_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class MyService
            {
                public MyService(string name) { }
            }

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
    public async Task Implicit_Parameterless_Constructor_Does_Not_Report()
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
    public async Task Protected_Constructor_Does_Not_Report()
    {
        // The generated impl derives from the target, so a protected constructor is chainable.
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public abstract class MyService
            {
                protected MyService(string name) { }
            }

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
    public async Task Internal_Constructor_In_Same_Assembly_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class MyService
            {
                internal MyService(string name) { }
            }

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
    public async Task Interface_Does_Not_Report()
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
    public async Task Sealed_Class_Does_Not_Report_TM006()
    {
        // TM001 already covers sealed types — TM006 must not pile on.
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public sealed class MyService
            {
                private MyService() { }
            }

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
}
