using Microsoft.CodeAnalysis.Testing;
using TUnit.Mocks.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mocks.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mocks.Analyzers.InaccessibleInterfaceMemberMockAnalyzer>;

namespace TUnit.Mocks.Analyzers.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6491
// An interface with a non-public abstract member cannot be implemented by any type the generator
// could emit, so the generator now skips it. TM007 is what turns that into an actionable error.
//
// The report's case is an `internal` member in a referenced assembly; these tests use `protected`
// members, which are unimplementable by a class in any assembly and so exercise the same
// accessibility check within a single compilation. The cross-assembly `internal` case is covered
// end-to-end by Issue6491Tests in TUnit.Mocks.SourceGenerator.Tests.
public class InaccessibleInterfaceMemberMockAnalyzerTests
{
    private const string MockStub = """
        namespace TUnit.Mocks
        {
            public static class Mock
            {
                public static object Of<T>() => default!;
                public static object Of<T>(int behavior) => default!;
            }
        }
        """;

    [Test]
    public async Task Interface_With_Inaccessible_Property_Reports_TM007()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface ISessionConverter
            {
                protected string MissingProperties { get; set; }

                string Describe();
            }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ISessionConverter>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("ISessionConverter", "MissingProperties")
        );
    }

    [Test]
    public async Task Interface_With_Inaccessible_Method_Reports_TM007()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface IHidden
            {
                protected void Secret();
            }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<IHidden>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("IHidden", "Secret")
        );
    }

    [Test]
    public async Task Inaccessible_Member_Inherited_From_Base_Interface_Reports_TM007()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface IBaseWithHiddenMember
            {
                protected string Hidden { get; }
            }

            public interface IDocumentStore : IBaseWithHiddenMember
            {
                string Identifier { get; }
            }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<IDocumentStore>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("IDocumentStore", "Hidden")
        );
    }

    [Test]
    public async Task Static_Mock_Entry_Point_Reports_TM007()
    {
        // The generator emits no `Mock()` member for an unmockable interface, so this form is
        // matched syntactically — `T.Mock()` with T resolving to the interface.
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface IHidden
            {
                protected void Secret();
            }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:IHidden.Mock()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("IHidden", "Secret"),
            // No mock is generated for this interface, so `Mock` is genuinely undefined here.
            DiagnosticResult.CompilerError("CS0117").WithSpan(18, 17, 18, 21).WithArguments("IHidden", "Mock")
        );
    }

    [Test]
    public async Task Interface_With_Only_Public_Members_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface IGreeter
            {
                string Greet(string name);
                string Name { get; set; }
                event System.Action Tick;
            }

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<IGreeter>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Non_Public_Member_With_Default_Implementation_Does_Not_Report()
    {
        // A member with a body is not the implementer's problem, so it never blocks mocking.
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface IWithDefault
            {
                protected string Helper() => "default";

                string Describe();
            }

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<IWithDefault>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Internal_Member_In_Same_Assembly_Does_Not_Report()
    {
        // Same-assembly internal members are implementable, so mocking stays supported.
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public interface IInternalMember
            {
                internal string Hidden { get; set; }

                string Describe();
            }

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<IInternalMember>();
                }
            }
            """
        );
    }

    [Test]
    public async Task Class_Target_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class MyService
            {
                public virtual string Describe() => "x";
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
