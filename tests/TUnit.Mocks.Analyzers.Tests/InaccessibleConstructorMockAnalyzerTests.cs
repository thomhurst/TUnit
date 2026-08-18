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
                public static object Of<T1, T2>() => default!;
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

    // Of<T1, T2>() returns Mock<T1>: T1 is the type the impl subclasses, and MockTypeDiscovery
    // reuses T1's constructors for the multi-type model. Without this the generator silently
    // skips generation and the call fails at runtime with "No mock factory registered" rather
    // than at the call site.
    [Test]
    public async Task Multi_Type_Of_With_Inaccessible_Primary_Constructor_Reports_TM006()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class RuntimeProperties
            {
                private RuntimeProperties(string name) { }
            }

            public interface ISomeInterface;

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<RuntimeProperties, ISomeInterface>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor)
                .WithLocation(0)
                .WithArguments("RuntimeProperties")
        );
    }

    [Test]
    public async Task Unrelated_Static_Mock_Method_Does_Not_Report()
    {
        // The generated entry point is a static member of an `extension(T)` block inside a
        // *_MockStaticExtension class. A static Mock() that merely happens to sit in namespace
        // TUnit.Mocks is not ours and must not draw a compilation-blocking diagnostic.
        //
        // The positive case can't be expressed here: the Roslyn behind this harness predates C# 14
        // extension blocks. It is covered by a real-SDK build in TUnit.Mocks.Tests, where
        // `T.Mock()` on a constructor-less class reports TM006 and no longer produces CS1729.
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
                    TUnit.Mocks.RuntimeProperties.Mock();
                }
            }
            """
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
    public async Task Cross_Assembly_Internal_Constructor_Reports_TM006()
    {
        // The shape from the report: Azure's QueueRuntimeProperties exposes only an internal
        // constructor, which is unreachable from a test assembly.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ExternalLib.QueueRuntimeProperties>()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public class QueueRuntimeProperties
                {
                    internal QueueRuntimeProperties(string name) { }

                    public virtual string Name { get; } = "";
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor)
                .WithLocation(0)
                .WithArguments("QueueRuntimeProperties")
        );
    }

    [Test]
    public async Task Cross_Assembly_Public_Constructor_Does_Not_Report()
    {
        // Guards the parameter-type accessibility check against false positives.
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<ExternalLib.OpenService>();
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public sealed class Options { }

                public class OpenService
                {
                    public OpenService(Options options) { }
                }
            }
            """
        );
    }

    [Test]
    public async Task Cross_Assembly_Protected_Internal_Parameter_Reports_TM006()
    {
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ExternalLib.GrpcClient>()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public class GrpcClient
                {
                    protected GrpcClient(ClientBaseConfiguration configuration) { }

                    protected internal class ClientBaseConfiguration { }
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor)
                .WithLocation(0)
                .WithArguments("GrpcClient")
        );
    }

    [Test]
    public async Task Cross_Assembly_Protected_Internal_Parameter_With_Parameterless_Fallback_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<ExternalLib.GrpcClient>();
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public class GrpcClient
                {
                    protected GrpcClient() { }

                    protected GrpcClient(ClientBaseConfiguration configuration) { }

                    protected internal class ClientBaseConfiguration { }
                }
            }
            """
        );
    }

    [Test]
    public async Task Protected_Only_Parameter_In_Same_Assembly_Reports_TM006()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class Service
            {
                protected Service(State state) { }

                protected class State { }
            }

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<Service>()|};
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor)
                .WithLocation(0)
                .WithArguments("Service")
        );
    }

    [Test]
    public async Task Inaccessible_Parameter_Inside_Generic_Array_Reports_TM006()
    {
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    {|#0:TUnit.Mocks.Mock.Of<ExternalLib.CompositeClient>()|};
                }
            }
            """,
            """
            namespace ExternalLib
            {
                public class CompositeClient
                {
                    protected CompositeClient(System.Collections.Generic.List<State[]> states) { }

                    protected internal class State { }
                }
            }
            """,
            Verifier.Diagnostic(Rules.TM006_CannotMockTypeWithoutAccessibleConstructor)
                .WithLocation(0)
                .WithArguments("CompositeClient")
        );
    }

    [Test]
    public async Task Protected_Internal_Parameter_With_InternalsVisibleTo_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerWithLibraryAsync(
            MockStub + """

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<ExternalLib.FriendClient>();
                }
            }
            """,
            """
            using System.Runtime.CompilerServices;

            [assembly: InternalsVisibleTo("TestProject")]

            namespace ExternalLib
            {
                public class FriendClient
                {
                    protected FriendClient(State state) { }

                    protected internal class State { }
                }
            }
            """
        );
    }

    [Test]
    public async Task Protected_Internal_Parameter_In_Same_Assembly_Does_Not_Report()
    {
        await Verifier.VerifyAnalyzerAsync(
            MockStub + """

            public class Service
            {
                protected Service(State state) { }

                protected internal class State { }
            }

            public class TestClass
            {
                public void Test()
                {
                    TUnit.Mocks.Mock.Of<Service>();
                }
            }
            """
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
