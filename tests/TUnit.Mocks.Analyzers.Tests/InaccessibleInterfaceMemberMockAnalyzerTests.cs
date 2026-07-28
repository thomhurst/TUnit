using Microsoft.CodeAnalysis.Testing;
using TUnit.Mocks.Analyzers.Tests.Verifiers;

using Verifier = TUnit.Mocks.Analyzers.Tests.Verifiers.CSharpAnalyzerVerifier<TUnit.Mocks.Analyzers.InaccessibleInterfaceMemberMockAnalyzer>;

namespace TUnit.Mocks.Analyzers.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/6491
// An interface with an abstract member the consuming assembly cannot reach has no possible
// implementer, so the generator skips it. TM007 is what turns that into an actionable error.
//
// Only `internal` and `private protected` are assembly-scoped, so every reporting case needs a
// genuinely separate compilation. `protected` / `protected internal` members are implementable from
// any assembly through explicit interface implementation and must NOT report.
public class InaccessibleInterfaceMemberMockAnalyzerTests
{
    private const string MockStub = """
        namespace TUnit.Mocks
        {
            public static class Mock
            {
                public static object Of<T>() => default!;
                public static object Of<T1, T2>() => default!;
            }
        }
        """;

    [Test]
    public async Task Cross_Assembly_Internal_Property_Reports_TM007()
    {
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ExternalLib.ISessionConverter>()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface ISessionConverter
                {
                    internal string MissingProperties { get; set; }

                    string Describe();
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("ISessionConverter", "MissingProperties")
        );
    }

    [Test]
    public async Task Cross_Assembly_Internal_Method_Reports_TM007()
    {
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ExternalLib.IHidden>()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IHidden
                {
                    internal void Secret();
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
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ExternalLib.IDocumentStore>()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IBaseWithHiddenMember
                {
                    internal string Hidden { get; }
                }

                public interface IDocumentStore : IBaseWithHiddenMember
                {
                    string Identifier { get; }
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("IDocumentStore", "Hidden")
        );
    }

    [Test]
    public async Task Additional_Interface_Of_A_Multi_Type_Mock_Reports_TM007()
    {
        // The impl lists every additional interface in its base-type list, so T2 being
        // unimplementable takes the whole combo down — the diagnostic has to cover it.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ExternalLib.IFine, ExternalLib.IHidden>()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IFine
                {
                    string Describe();
                }

                public interface IHidden
                {
                    internal void Secret();
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("IHidden", "Secret")
        );
    }

    [Test]
    public async Task Static_Mock_Entry_Point_Reports_TM007()
    {
        // The generator emits no `Mock()` member for an unmockable interface, so this form is
        // matched syntactically — `T.Mock()` with T resolving to the interface.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:ExternalLib.IHidden.Mock()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IHidden
                {
                    internal void Secret();
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("IHidden", "Secret"),
            // No mock is generated for this interface, so `Mock` is genuinely undefined here.
            DiagnosticResult.CompilerError("CS0117").WithSpan(13, 29, 13, 33)
        );
    }

    [Test]
    public async Task Cross_Assembly_Internal_Setter_On_A_Public_Property_Reports_TM007()
    {
        // C# allows a per-accessor modifier on an interface property, so a reachable property can
        // still declare an unreachable slot. Reported against the property, which is what the
        // compiler error would name.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ExternalLib.IHalfHidden>()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IHalfHidden
                {
                    string Value { get; internal set; }
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("IHalfHidden", "Value")
        );
    }

    [Test]
    public async Task GenerateMock_Attribute_For_An_Unmockable_Interface_Reports_TM007()
    {
        // The attribute produces no invocation, so without its own action the request would be
        // dropped by the generator with nothing said.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            """
            [assembly: {|#0:TUnit.Mocks.GenerateMock(typeof(ExternalLib.IHidden))|}]

            namespace TUnit.Mocks
            {
                [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
                public sealed class GenerateMockAttribute : System.Attribute
                {
                    public GenerateMockAttribute(System.Type type) { }
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IHidden
                {
                    internal void Secret();
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM007_CannotMockInterfaceWithInaccessibleMember)
                .WithLocation(0)
                .WithArguments("IHidden", "Secret")
        );
    }

    [Test]
    public async Task Unrelated_Static_Mock_Method_Does_Not_Report()
    {
        // Another library's own `IFoo.Mock()` binds to a method that is not the generated entry
        // point, so it must not be treated as a TUnit mock request.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public static class OtherLibraryExtensions
            {
                public static object Mock(this ExternalLib.IHidden hidden) => hidden;
            }

            public class TestClass
            {
                public void Test(ExternalLib.IHidden hidden)
                {
                    hidden.Mock();
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IHidden
                {
                    internal void Secret();
                }
            }
            """
        );
    }

    [Test]
    public async Task Cross_Assembly_Protected_Member_Does_Not_Report()
    {
        // A protected interface member is implementable from any assembly via explicit interface
        // implementation, so mocking must keep working. Guards the accessibility check against
        // Compilation.IsSymbolAccessibleWithin(member, assembly), which reports protected members
        // as inaccessible and would silently drop these interfaces from generation.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<ExternalLib.IProtectedMember>();
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IProtectedMember
                {
                    protected string Hidden { get; set; }

                    string Describe();
                }
            }
            """
        );
    }

    [Test]
    public async Task Cross_Assembly_Protected_Internal_Member_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<ExternalLib.IProtectedInternalMember>();
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IProtectedInternalMember
                {
                    protected internal string Hidden { get; set; }

                    string Describe();
                }
            }
            """
        );
    }

    [Test]
    public async Task Cross_Assembly_Internal_Member_With_Default_Implementation_Does_Not_Report()
    {
        // A member with a body is not the implementer's problem, so it never blocks mocking.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<ExternalLib.IWithDefault>();
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public interface IWithDefault
                {
                    internal string Helper() => "default";

                    string Describe();
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
