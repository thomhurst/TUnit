using Microsoft.CodeAnalysis.CSharp;

using Verifier = TUnit.Mocks.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    TUnit.Mocks.Analyzers.PreferStaticMockSyntaxAnalyzer,
    TUnit.Mocks.Analyzers.CodeFixers.PreferStaticMockSyntaxCodeFixProvider>;

namespace TUnit.Mocks.Analyzers.Tests;

public class PreferStaticMockSyntaxAnalyzerTests
{
    private const string CoreStubs = """
        namespace TUnit.Mocks
        {
            public enum MockBehavior { Loose, Strict }

            public interface IDefaultValueProvider { }

            public class Mock<T> where T : class { }

            public static class Mock
            {
                public static Mock<T> Of<T>() where T : class => null!;
                public static Mock<T> Of<T>(MockBehavior behavior) where T : class => null!;
                public static Mock<T> Of<T>(MockBehavior behavior, IDefaultValueProvider defaultValueProvider) where T : class => null!;
                public static Mock<T> Of<T>(params object[] constructorArgs) where T : class => null!;
                public static Mock<T> Of<T>(MockBehavior behavior, params object[] constructorArgs) where T : class => null!;
                public static Mock<T1> Of<T1, T2>() where T1 : class where T2 : class => null!;
            }

            public class MockRepository
            {
                public Mock<T> Of<T>() where T : class => null!;
            }

            public static class Arg { }
        }

        namespace OtherLibrary
        {
            public static class Arg { }
        }

        namespace App
        {
            public interface IFoo { }
            public interface IBar { }
            public interface IGeneric<T> { }
            public class Service
            {
                public Service() { }
                public Service(int value) { }
            }
        }
        """;

    // Stand-ins for the T.Mock() extensions TUnit.Mocks.SourceGenerator emits, so the fixed code
    // is compiled for real.
    private const string Stubs = CoreStubs + """

        namespace TUnit.Mocks
        {
            public static class IFoo_MockStaticExtension
            {
                extension(global::App.IFoo)
                {
                    public static global::TUnit.Mocks.Mock<global::App.IFoo> Mock() => null!;
                    public static global::TUnit.Mocks.Mock<global::App.IFoo> Mock(global::TUnit.Mocks.MockBehavior behavior) => null!;
                }
            }

            public static class Service_MockStaticExtension
            {
                extension(global::App.Service)
                {
                    public static global::TUnit.Mocks.Mock<global::App.Service> Mock() => null!;
                    public static global::TUnit.Mocks.Mock<global::App.Service> Mock(global::TUnit.Mocks.MockBehavior behavior) => null!;
                }
            }

            public static class IGeneric_MockStaticExtension
            {
                extension<T>(global::App.IGeneric<T>)
                {
                    public static global::TUnit.Mocks.Mock<global::App.IGeneric<T>> Mock() => null!;
                }
            }
        }
        """;

    [Test]
    public async Task Interface_Is_Rewritten()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    var mock = {|#0:Mock.Of<IFoo>()|};
                }
            }
            """,
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    var mock = IFoo.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"));
    }

    [Test]
    public async Task Behavior_Argument_Is_Kept()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    var strict = {|#0:Mock.Of<IFoo>(MockBehavior.Strict)|};
                    var named = {|#1:Mock.Of<Service>(behavior: MockBehavior.Loose)|};
                }
            }
            """,
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    var strict = IFoo.Mock(MockBehavior.Strict);
                    var named = Service.Mock(behavior: MockBehavior.Loose);
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"),
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(1).WithArguments("Service"));
    }

    [Test]
    public async Task Class_And_Closed_Generic_Are_Rewritten()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    Mock<Service> service = {|#0:Mock.Of<Service>()|};
                    Mock<IGeneric<int>> generic = {|#1:Mock.Of<IGeneric<int>>()|};
                }
            }
            """,
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    Mock<Service> service = Service.Mock();
                    Mock<IGeneric<int>> generic = IGeneric<int>.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("Service"),
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(1).WithArguments("IGeneric<int>"));
    }

    [Test]
    public async Task Qualified_Names_Are_Kept_And_Using_Is_Added()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            public class Tests
            {
                public void Test()
                {
                    var foo = {|#0:TUnit.Mocks.Mock.Of<App.IFoo>()|};
                    var service = {|#1:global::TUnit.Mocks.Mock.Of<global::App.Service>()|};
                }
            }
            """,
            """
            using TUnit.Mocks;

            public class Tests
            {
                public void Test()
                {
                    var foo = App.IFoo.Mock();
                    var service = global::App.Service.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"),
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(1).WithArguments("Service"));
    }

    [Test]
    public async Task Alias_To_Mock_Type_Adds_Using()
    {
        // An alias imports the type only, not the TUnit.Mocks extension members.
        await Verifier.VerifyCodeFixAsync(
            """
            using App;
            using M = TUnit.Mocks.Mock;

            public class Tests
            {
                public void Test()
                {
                    var foo = {|#0:M.Of<IFoo>()|};
                }
            }
            """,
            """
            using App;
            using M = TUnit.Mocks.Mock;
            using TUnit.Mocks;

            public class Tests
            {
                public void Test()
                {
                    var foo = IFoo.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"));
    }

    [Test]
    public async Task Nullable_Annotation_Is_Dropped()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    var mock = {|#0:Mock.Of<IFoo?>()|};
                }
            }
            """,
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    var mock = IFoo.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"));
    }

    [Test]
    public async Task Using_Static_Mock_Is_Rewritten()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;
            using static TUnit.Mocks.Mock;

            public class Tests
            {
                public void Test()
                {
                    var mock = {|#0:Of<IFoo>()|};
                }
            }
            """,
            """
            using TUnit.Mocks;
            using App;
            using static TUnit.Mocks.Mock;

            public class Tests
            {
                public void Test()
                {
                    var mock = IFoo.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"));
    }

    [Test]
    public async Task Code_Inside_TUnit_Mocks_Namespace_Needs_No_Using()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            namespace TUnit.Mocks.Tests
            {
                public class Tests
                {
                    public void Test()
                    {
                        var mock = {|#0:Mock.Of<App.IFoo>()|};
                    }
                }
            }
            """,
            """
            namespace TUnit.Mocks.Tests
            {
                public class Tests
                {
                    public void Test()
                    {
                        var mock = App.IFoo.Mock();
                    }
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"));
    }

    [Test]
    public async Task Multiple_Calls_In_One_Statement_Are_All_Rewritten()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Use(object first, object second) { }

                public void Test()
                {
                    Use({|#0:Mock.Of<IFoo>()|}, {|#1:Mock.Of<Service>(MockBehavior.Strict)|});
                }
            }
            """,
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Use(object first, object second) { }

                public void Test()
                {
                    Use(IFoo.Mock(), Service.Mock(MockBehavior.Strict));
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"),
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(1).WithArguments("Service"));
    }

    [Test]
    public async Task Reassigned_Var_Local_Of_Interface_Is_Not_Reported()
    {
        // For interfaces T.Mock() returns the generated wrapper type, so a `var` local it
        // initialises would change type and the later reassignment from a Mock<IFoo> wouldn't
        // compile. The assignment itself is still safe to rewrite.
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test(Mock<IFoo> other)
                {
                    var mock = Mock.Of<IFoo>();
                    mock = other;
                    mock = {|#0:Mock.Of<IFoo>()|};
                }
            }
            """,
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test(Mock<IFoo> other)
                {
                    var mock = Mock.Of<IFoo>();
                    mock = other;
                    mock = IFoo.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"));
    }

    [Test]
    public async Task Interface_Calls_In_Inferred_Type_Contexts_Are_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using System;
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                private static T Identity<T>(T value) => value;

                public void Test(bool flag, Mock<IFoo> other)
                {
                    var array = new[] { Mock.Of<IFoo>() };
                    var lambda = () => Mock.Of<IFoo>();
                    Func<object> block = () => { return Mock.Of<IFoo>(); };
                    var inferred = Identity(Mock.Of<IFoo>());
                    var tuple = (Mock.Of<IFoo>(), 1);
                    var conditional = flag ? Mock.Of<IFoo>() : Mock.Of<IFoo>(MockBehavior.Strict);
                    conditional = other;
                }
            }
            """,
            Stubs);
    }

    [Test]
    public async Task Reassigned_Top_Level_Var_Local_Is_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using TUnit.Mocks;
            using App;

            var mock = Mock.Of<IFoo>();
            mock = Other();

            static Mock<IFoo> Other() => null!;
            """,
            Stubs,
            outputKind: Microsoft.CodeAnalysis.OutputKind.ConsoleApplication);
    }

    [Test]
    public async Task Receiver_Shadowed_By_Member_Is_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                private object IFoo => null!;

                public void Test()
                {
                    Mock<IFoo> mock = Mock.Of<IFoo>();
                }
            }
            """,
            Stubs);
    }

    [Test]
    public async Task Not_Reported_When_Adding_Using_Would_Cause_Ambiguity()
    {
        // `using TUnit.Mocks;` would make the existing `Arg` ambiguous (CS0104).
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using OtherLibrary;

            public class Tests
            {
                public void Test()
                {
                    var arg = typeof(Arg);
                    TUnit.Mocks.Mock<App.IFoo> mock = TUnit.Mocks.Mock.Of<App.IFoo>();
                }
            }
            """,
            Stubs);
    }

    [Test]
    public async Task Own_Namespace_Type_Does_Not_Block_Adding_Using()
    {
        // Types of the enclosing namespace take precedence over a using import, so there is no clash.
        await Verifier.VerifyCodeFixAsync(
            """
            namespace OtherLibrary
            {
                public class Tests
                {
                    public void Test()
                    {
                        var arg = typeof(Arg);
                        TUnit.Mocks.Mock<App.IFoo> mock = {|#0:TUnit.Mocks.Mock.Of<App.IFoo>()|};
                    }
                }
            }
            """,
            """
            using TUnit.Mocks;

            namespace OtherLibrary
            {
                public class Tests
                {
                    public void Test()
                    {
                        var arg = typeof(Arg);
                        TUnit.Mocks.Mock<App.IFoo> mock = App.IFoo.Mock();
                    }
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM010_PreferStaticMockSyntax).WithLocation(0).WithArguments("IFoo"));
    }

    [Test]
    public async Task Unsupported_Overloads_Are_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test(IDefaultValueProvider provider, MockRepository repository)
                {
                    var multi = Mock.Of<IFoo, IBar>();
                    var withProvider = Mock.Of<IFoo>(MockBehavior.Strict, provider);
                    var withArgs = Mock.Of<Service>(42);
                    var withBehaviorAndArgs = Mock.Of<Service>(MockBehavior.Strict, 42);
                    var fromRepository = repository.Of<IFoo>();
                }
            }
            """,
            Stubs);
    }

    [Test]
    public async Task Types_Without_A_Generated_Static_Mock_Are_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using System;
            using TUnit.Mocks;
            using App;

            public sealed class SealedService { }

            public class HasOwnMock
            {
                public static object Mock() => null!;
            }

            public interface IWithRefStructProperty
            {
                ReadOnlySpan<byte> Bytes { get; }
            }

            public class Tests
            {
                private interface IPrivateNested { }

                public void Test<T>() where T : class
                {
                    var typeParameter = Mock.Of<T>();
                    var openGeneric = Mock.Of<IGeneric<T>>();
                    var sealedType = Mock.Of<SealedService>();
                    var ownMock = Mock.Of<HasOwnMock>();
                    var refStruct = Mock.Of<IWithRefStructProperty>();
                    var privateNested = Mock.Of<IPrivateNested>();
                    var @delegate = Mock.Of<Action>();
                }
            }
            """,
            Stubs);
    }

    [Test]
    public async Task Below_CSharp_14_Is_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    var mock = Mock.Of<IFoo>();
                }
            }
            """,
            CoreStubs,
            LanguageVersion.CSharp13);
    }
}
