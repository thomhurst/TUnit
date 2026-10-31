using Verifier = TUnit.Mocks.Analyzers.Tests.Verifiers.CSharpCodeFixVerifier<
    TUnit.Mocks.Analyzers.PreferGeneratedMockTypeAnalyzer,
    TUnit.Mocks.Analyzers.CodeFixers.PreferGeneratedMockTypeCodeFixProvider>;

namespace TUnit.Mocks.Analyzers.Tests;

public class PreferGeneratedMockTypeAnalyzerTests
{
    // Stand-ins for TUnit.Mocks and the code TUnit.Mocks.SourceGenerator emits. Like the real
    // generator, an interface's T.Mock() returns a sealed wrapper in the interface's namespace that
    // derives from Mock<T> and implements T; a class's T.Mock() returns Mock<T>.
    private const string Stubs = """
        namespace TUnit.Mocks
        {
            public enum MockBehavior { Loose, Strict }

            public class Mock<T> where T : class
            {
                public T Object => null!;
            }

            public static class Mock
            {
                public static Mock<T> Of<T>() where T : class => null!;
            }

            public static class IFoo_MockStaticExtension
            {
                extension(global::App.IFoo)
                {
                    public static global::App.IFooMock Mock() => null!;
                    public static global::App.IFooMock Mock(global::TUnit.Mocks.MockBehavior behavior) => null!;
                }
            }

            public static class IGeneric_MockStaticExtension
            {
                extension<T>(global::App.IGeneric<T>)
                {
                    public static global::App.IGenericMock<T> Mock() => null!;
                }
            }

            public static class Service_MockStaticExtension
            {
                extension(global::App.Service)
                {
                    public static global::TUnit.Mocks.Mock<global::App.Service> Mock() => null!;
                }
            }
        }

        namespace App
        {
            public interface IFoo { }
            public interface IGeneric<T> { }
            public class Service { }

            public sealed class IFooMock : global::TUnit.Mocks.Mock<IFoo>, IFoo { }
            public sealed class IGenericMock<T> : global::TUnit.Mocks.Mock<IGeneric<T>>, IGeneric<T> { }
        }
        """;

    [Test]
    public async Task Local_Is_Retyped()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    {|#0:Mock<IFoo>|} mock = IFoo.Mock();
                    {|#1:Mock<IFoo>|} strict = IFoo.Mock(MockBehavior.Strict);
                    var foo = mock.Object;
                    Use(strict);
                }

                private static void Use(Mock<IFoo> mock) { }
            }
            """,
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    IFooMock mock = IFoo.Mock();
                    IFooMock strict = IFoo.Mock(MockBehavior.Strict);
                    var foo = mock.Object;
                    Use(strict);
                }

                private static void Use(Mock<IFoo> mock) { }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(0).WithArguments("mock", "IFooMock", "Mock<IFoo>"),
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(1).WithArguments("strict", "IFooMock", "Mock<IFoo>"));
    }

    [Test]
    public async Task Private_Fields_Assigned_Later_Are_Retyped()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            #nullable enable
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                private {|#0:Mock<IFoo>|} _mock = null!;
                private {|#1:Mock<IFoo>?|} _optional;
                private static readonly {|#2:Mock<IFoo>|} Shared = IFoo.Mock();

                public void Setup()
                {
                    _mock = IFoo.Mock();
                    this._optional ??= IFoo.Mock();
                }

                public IFoo Test() => _mock.Object;

                public Mock<IFoo> Get() => Shared;
            }
            """,
            """
            #nullable enable
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                private IFooMock _mock = null!;
                private IFooMock? _optional;
                private static readonly IFooMock Shared = IFoo.Mock();

                public void Setup()
                {
                    _mock = IFoo.Mock();
                    this._optional ??= IFoo.Mock();
                }

                public IFoo Test() => _mock.Object;

                public Mock<IFoo> Get() => Shared;
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(0).WithArguments("_mock", "IFooMock", "Mock<IFoo>"),
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(1).WithArguments("_optional", "IFooMock", "Mock<IFoo>"),
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(2).WithArguments("Shared", "IFooMock", "Mock<IFoo>"));
    }

    [Test]
    public async Task Private_Properties_Are_Retyped()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                private {|#0:Mock<IFoo>|} Initialized { get; } = IFoo.Mock();
                private {|#1:Mock<IFoo>|} Assigned { get; set; }
                private {|#2:Mock<IFoo>|} Computed => IFoo.Mock();

                public void Setup()
                {
                    Assigned = IFoo.Mock();
                    var objects = new[] { Initialized.Object, Assigned.Object, Computed.Object };
                }
            }
            """,
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                private IFooMock Initialized { get; } = IFoo.Mock();
                private IFooMock Assigned { get; set; }
                private IFooMock Computed => IFoo.Mock();

                public void Setup()
                {
                    Assigned = IFoo.Mock();
                    var objects = new[] { Initialized.Object, Assigned.Object, Computed.Object };
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(0).WithArguments("Initialized", "IFooMock", "Mock<IFoo>"),
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(1).WithArguments("Assigned", "IFooMock", "Mock<IFoo>"),
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(2).WithArguments("Computed", "IFooMock", "Mock<IFoo>"));
    }

    [Test]
    public async Task Generic_And_Qualified_Types_Are_Written_As_Visible_From_The_Declaration()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;

            public class Tests
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock<App.IFoo>|} qualified = App.IFoo.Mock();
                    {|#1:Mock<App.IGeneric<string>>|} generic = App.IGeneric<string>.Mock();
                }
            }
            """,
            """
            using TUnit.Mocks;

            public class Tests
            {
                public void Test()
                {
                    App.IFooMock qualified = App.IFoo.Mock();
                    App.IGenericMock<string> generic = App.IGeneric<string>.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(0).WithArguments("qualified", "App.IFooMock", "Mock<App.IFoo>"),
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(1).WithArguments("generic", "App.IGenericMock<string>", "Mock<App.IGeneric<string>>"));
    }

    [Test]
    public async Task Multiple_Declarators_Are_Retyped_Together()
    {
        await Verifier.VerifyCodeFixAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                public void Test()
                {
                    {|#0:Mock<IFoo>|} first = IFoo.Mock(), second = IFoo.Mock();
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
                    IFooMock first = IFoo.Mock(), second = IFoo.Mock();
                }
            }
            """,
            Stubs,
            Verifier.Diagnostic(Rules.TM011_PreferGeneratedMockType).WithLocation(0).WithArguments("first, second", "IFooMock", "Mock<IFoo>"));
    }

    [Test]
    public async Task Members_Outside_The_Analyzers_View_Are_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using TUnit.Mocks;
            using App;

            public partial class Tests
            {
                public Mock<IFoo> PublicField = IFoo.Mock();
                protected Mock<IFoo> ProtectedField = IFoo.Mock();
                internal Mock<IFoo> InternalField = IFoo.Mock();
                public Mock<IFoo> PublicProperty { get; } = IFoo.Mock();

                // Another part of this partial class, in another file, could assign it.
                private Mock<IFoo> _partial = IFoo.Mock();
            }
            """,
            Stubs + """

            public partial class Tests { }
            """);
    }

    [Test]
    public async Task Type_Sensitive_Uses_Are_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using System;
            using System.Threading.Tasks;
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                private Mock<IFoo> _reassigned = IFoo.Mock();
                private Mock<IFoo> _inferred = IFoo.Mock();
                private Mock<IFoo> _byRef = IFoo.Mock();
                private Mock<IFoo> _lambda = IFoo.Mock();
                private Mock<IFoo> _deconstructed = IFoo.Mock();
                private Mock<IFoo> _copied = IFoo.Mock();
                private Mock<IFoo> _withBody { get => IFoo.Mock(); }

                public void Test(Tests other)
                {
                    _reassigned = Mock.Of<IFoo>();
                    Task<Mock<IFoo>> task = Task.FromResult(_inferred);
                    Change(ref _byRef);
                    Func<Mock<IFoo>> factory = () => _lambda;
                    (_deconstructed, var unused) = (IFoo.Mock(), 1);
                    var copy = _copied;
                    copy = Mock.Of<IFoo>();
                }

                private static void Change(ref Mock<IFoo> mock) { }
            }
            """,
            Stubs);
    }

    [Test]
    public async Task Values_That_Are_Not_The_Generated_Type_Are_Not_Reported()
    {
        await Verifier.VerifyNoDiagnosticAsync(
            """
            using TUnit.Mocks;
            using App;

            public class Tests
            {
                private Mock<IFoo> _onlyNull = null!;
                private Mock<IFoo> _fromOf = Mock.Of<IFoo>();

                public void Test(Mock<IFoo> parameter)
                {
                    Mock<Service> classMock = Service.Mock();
                    Mock<IFoo> fromParameter = parameter;
                    var inferred = IFoo.Mock();
                    IFoo asInterface = IFoo.Mock();
                    object asObject = IFoo.Mock();
                }
            }
            """,
            Stubs);
    }
}
