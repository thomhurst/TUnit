using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Regression: https://github.com/thomhurst/TUnit/issues/6634
/// Generated subclasses can name protected nested types in constructor and member signatures.
/// Their non-derived factories and setup extensions cannot, so those public surfaces must omit
/// inaccessible signatures without dropping overrides required to instantiate abstract clients.
/// </summary>
public class Issue6634Tests : SnapshotTestBase
{
    private const string GrpcStyleExternalLibrary = """
        namespace ExternalLib;

        public abstract class GrpcClient
        {
            protected GrpcClient() { }

            protected abstract GrpcClient NewInstance(ClientBaseConfiguration configuration);

            protected internal class ClientBaseConfiguration { }

            public GrpcClient Clone() => NewInstance(new ClientBaseConfiguration());

            public abstract string Call();
        }
        """;

    [Test]
    public async Task Grpc_Style_Constructor_With_Protected_Internal_State_Is_Omitted()
    {
        var generated = await GenerateFromExternalLibrary("""
            namespace ExternalLib;

            public class GrpcClient
            {
                protected GrpcClient() { }

                protected GrpcClient(ClientBaseConfiguration configuration) { }

                protected internal class ClientBaseConfiguration { }

                public virtual string Call() => "real";
            }
            """, "ExternalLib.GrpcClient");

        await Assert.That(generated).Contains("engine) : base()");
        await Assert.That(generated).DoesNotContain("ClientBaseConfiguration");
    }

    [Test]
    public async Task Grpc_Style_Abstract_Method_With_Protected_Internal_State_Is_Mockable()
    {
        var reference = CreateExternalAssemblyReference(GrpcStyleExternalLibrary);
        var source = GrpcMockSource();
        var generated = string.Join(Environment.NewLine, RunGenerator(source, [reference]));

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(generated).Contains(
            "protected override global::ExternalLib.GrpcClient NewInstance(global::ExternalLib.GrpcClient.ClientBaseConfiguration configuration)");
        await Assert.That(generated).Contains("public override string Call()");
    }

    [Test]
    public Task Grpc_Style_Abstract_Method_Generation_Snapshot()
    {
        var reference = CreateExternalAssemblyReference(GrpcStyleExternalLibrary);

        return VerifyGeneratorOutput(GrpcMockSource(), [reference]);
    }

    [Test]
    public async Task Composite_Inaccessible_Method_Parameter_Is_Mockable()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public abstract class CompositeClient
            {
                protected CompositeClient() { }

                protected abstract CompositeClient NewInstance(
                    System.Collections.Generic.IReadOnlyDictionary<string, State[]> states);

                protected internal class State { }
            }
            """);
        var source = MockSource("ExternalLib.CompositeClient");

        await AssertNoAccessibilityErrors(source, reference);
    }

    [Test]
    public async Task Inaccessible_Method_Return_Type_Is_Mockable()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public abstract class StateClient
            {
                protected StateClient() { }

                protected abstract State GetState();

                protected internal class State { }
            }
            """);
        var source = MockSource("ExternalLib.StateClient");

        await AssertNoAccessibilityErrors(source, reference);
    }

    [Test]
    public async Task Protected_Method_Parameter_Is_Omitted_Even_In_Same_Assembly()
    {
        var source = """
            using TUnit.Mocks;

            public abstract class LocalClient
            {
                protected LocalClient() { }

                protected abstract LocalClient NewInstance(State state);

                protected class State { }
            }

            public class Test
            {
                public void Run() => Mock.Of<LocalClient>();
            }
            """;

        await AssertNoAccessibilityErrors(source);
    }

    [Test]
    public async Task Inaccessible_Generic_Constraint_Is_Omitted_From_Member_Surface()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public abstract class GenericClient
            {
                protected GenericClient() { }

                protected abstract void Handle<T>(T value) where T : State;

                protected internal class State { }

                public abstract void Ping<T>(T value);
            }
            """);
        var source = MockSource("ExternalLib.GenericClient");
        var sources = RunGenerator(source, [reference]);
        var memberSurface = GetMemberSurface(sources);

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(string.Join(Environment.NewLine, sources)).Contains("protected override void Handle<T>(T value)");
        await Assert.That(memberSurface).DoesNotContain("Handle");
        await Assert.That(memberSurface).Contains("Ping<T>");
    }

    [Test]
    public async Task Inaccessible_Overload_Does_Not_Suppress_AnyArgs_Helper()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public abstract class OverloadClient
            {
                protected OverloadClient() { }

                public abstract string Search(string query, int count);

                protected abstract string Search(State state, int count);

                protected internal class State { }
            }
            """);
        var source = MockSource("ExternalLib.OverloadClient");
        var memberSurface = GetMemberSurface(RunGenerator(source, [reference]));

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(memberSurface).Contains(
            "Search(this global::TUnit.Mocks.Mock<global::ExternalLib.OverloadClient> mock, global::TUnit.Mocks.Arguments.AnyArgs _)");
    }

    [Test]
    public async Task Inaccessible_Params_Overload_Does_Not_Suppress_AnyArg_Helper()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public abstract class ParamsClient
            {
                protected ParamsClient() { }

                public abstract int Pack(params int[] values);

                protected abstract int Pack(params State[] values);

                protected internal class State { }
            }
            """);
        var source = MockSource("ExternalLib.ParamsClient");
        var memberSurface = GetMemberSurface(RunGenerator(source, [reference]));

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(memberSurface).Contains("global::TUnit.Mocks.Arguments.AnyArg values");
    }

    [Test]
    public async Task Inaccessible_Property_Type_Is_Omitted_From_Member_Surface()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public abstract class PropertyClient
            {
                protected PropertyClient() { }

                protected abstract State Hidden { get; }

                public abstract string Visible { get; }

                protected internal class State { }
            }
            """);
        var source = MockSource("ExternalLib.PropertyClient");
        var memberSurface = GetMemberSurface(RunGenerator(source, [reference]));

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(memberSurface).DoesNotContain("Hidden");
        await Assert.That(memberSurface).Contains("Visible");
    }

    [Test]
    public async Task Inaccessible_Indexer_Types_Are_Omitted_From_Member_Surface()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public abstract class IndexerClient
            {
                protected IndexerClient() { }

                protected abstract State this[State state] { get; }

                public abstract string this[int index] { get; }

                protected internal class State { }
            }
            """);
        var source = MockSource("ExternalLib.IndexerClient");
        var memberSurface = GetMemberSurface(RunGenerator(source, [reference]));

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(memberSurface).DoesNotContain("IndexerClient.State");
        await Assert.That(memberSurface).Contains("global::TUnit.Mocks.Arguments.Arg<int> index");
    }

    [Test]
    public async Task Inaccessible_Event_Type_Has_No_Typed_Raise_Surface()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public abstract class EventClient
            {
                protected EventClient() { }

                protected abstract event System.EventHandler<State>? Hidden;

                public abstract event System.EventHandler? Visible;

                protected internal class State { }
            }
            """);
        var source = MockSource("ExternalLib.EventClient");
        var memberSurface = GetMemberSurface(RunGenerator(source, [reference]));

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(memberSurface).DoesNotContain("RaiseHidden");
        await Assert.That(memberSurface).Contains("RaiseVisible");
    }

    [Test]
    public async Task Inaccessible_Method_Emits_No_Dangling_RefStruct_Setter_Delegate()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public ref struct Buffer { }

            public abstract class ReaderClient
            {
                protected ReaderClient() { }

                protected abstract void Read(State state, ref Buffer buffer);

                protected internal class State { }
            }
            """);
        var source = MockSource("ExternalLib.ReaderClient");
        var generated = string.Join(Environment.NewLine, RunGenerator(source, [reference]));

        await AssertNoCompilerErrors(source, reference, "CS0051", "CS0122", "CS0246");
        await Assert.That(generated).DoesNotContain("ReaderClient_Read_M0_Buffer_RefSetter");
    }

    [Test]
    public async Task Unresolved_Signature_Type_Does_Not_Abort_Generation_Or_Leak_Into_Surface()
    {
        var source = """
            using TUnit.Mocks;

            public abstract class ErrorClient
            {
                public abstract MissingType Transform(MissingType value);
            }

            public class Test
            {
                public void Run() => Mock.Of<ErrorClient>();
            }
            """;

        var (sources, diagnostics) = RunGeneratorForDiagnostics(source);
        var generatorErrors = diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        var memberSurface = GetMemberSurface(sources);

        await Assert.That(generatorErrors).IsEmpty();
        await Assert.That(string.Join(Environment.NewLine, sources)).Contains("Transform");
        await Assert.That(memberSurface).DoesNotContain("Transform");
    }

    [Test]
    public async Task Inaccessible_Types_Are_Found_Inside_Arrays_And_Generic_Arguments()
    {
        var generated = await GenerateFromExternalLibrary("""
            namespace ExternalLib;

            public class CompositeClient
            {
                protected CompositeClient() { }

                protected CompositeClient(System.Collections.Generic.List<State[]> states) { }

                protected internal class State { }
            }
            """, "ExternalLib.CompositeClient");

        await Assert.That(generated).DoesNotContain("global::System.Collections.Generic.List<global::ExternalLib.CompositeClient.State[]>");
    }

    [Test]
    public async Task Sole_Constructor_With_Inaccessible_Parameter_Emits_Only_Diagnostic_Stub()
    {
        var generated = await GenerateFromExternalLibrary("""
            namespace ExternalLib;

            public class UnconstructableClient
            {
                protected UnconstructableClient(State state) { }

                protected internal class State { }
            }
            """, "ExternalLib.UnconstructableClient");

        await Assert.That(generated).Contains("UnconstructableClient_MockStaticExtension");
        await Assert.That(generated).DoesNotContain("UnconstructableClientMockImpl");
    }

    [Test]
    public async Task Protected_Only_Parameter_Is_Omitted_Even_In_Same_Assembly()
    {
        var source = """
            using TUnit.Mocks;

            public class LocalClient
            {
                protected LocalClient() { }

                protected LocalClient(State state) { }

                protected class State { }
            }

            public class Test
            {
                public void Run() => Mock.Of<LocalClient>();
            }
            """;

        var generated = string.Join(Environment.NewLine, RunGenerator(source));

        await Assert.That(generated).Contains("engine) : base()");
        await Assert.That(generated).DoesNotContain("LocalClient.State");
    }

    [Test]
    public async Task Internally_Accessible_Parameter_Uses_Internal_Extension_Overload()
    {
        var externalLibrary = """
            using System.Runtime.CompilerServices;

            [assembly: InternalsVisibleTo("TestAssembly")]

            namespace ExternalLib;

            public class FriendClient
            {
                protected FriendClient() { }

                protected FriendClient(State state) { }

                protected internal class State { }
            }
            """;

        var reference = CreateExternalAssemblyReference(externalLibrary);
        var source = MockSource("ExternalLib.FriendClient");
        var generated = string.Join(Environment.NewLine, RunGenerator(source, [reference]));

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(generated).Contains("internal static global::TUnit.Mocks.Mock<global::ExternalLib.FriendClient> Mock(global::ExternalLib.FriendClient.State state)");
        await Assert.That(generated).DoesNotContain("public static global::TUnit.Mocks.Mock<global::ExternalLib.FriendClient> Mock(global::ExternalLib.FriendClient.State state)");
    }

    [Test]
    public async Task Same_Assembly_Protected_Internal_Parameter_Uses_Internal_Extension_Overload()
    {
        var source = """
            using TUnit.Mocks;

            public class LocalFriendClient
            {
                protected LocalFriendClient() { }

                protected LocalFriendClient(State state) { }

                protected internal class State { }
            }

            public class Test
            {
                public void Run() => Mock.Of<LocalFriendClient>();
            }
            """;

        var generated = string.Join(Environment.NewLine, RunGenerator(source));

        await AssertNoAccessibilityErrors(source);
        await Assert.That(generated).Contains("internal static global::TUnit.Mocks.Mock<global::LocalFriendClient> Mock(global::LocalFriendClient.State state)");
    }

    [Test]
    public async Task Public_Constructor_Parameter_Keeps_Public_Extension_Overload()
    {
        var generated = await GenerateFromExternalLibrary("""
            namespace ExternalLib;

            public class PublicClient
            {
                protected PublicClient() { }

                protected PublicClient(Options options) { }

                public class Options { }
            }
            """, "ExternalLib.PublicClient");

        await Assert.That(generated).Contains("public static global::TUnit.Mocks.Mock<global::ExternalLib.PublicClient> Mock(global::ExternalLib.PublicClient.Options options)");
    }

    [Test]
    public async Task Wrap_Preserves_Constructor_With_Protected_Parameter()
    {
        var reference = CreateExternalAssemblyReference("""
            namespace ExternalLib;

            public class WrappedClient
            {
                protected WrappedClient(State state) { }

                protected class State { }

                public virtual string Call() => "real";
            }
            """);
        var source = """
            using TUnit.Mocks;

            public class Test
            {
                public void Run(ExternalLib.WrappedClient instance) => Mock.Wrap(instance);
            }
            """;

        var generated = string.Join(Environment.NewLine, RunGenerator(source, [reference]));

        await AssertNoAccessibilityErrors(source, reference);
        await Assert.That(generated).Contains("WrappedClientWrapMockImpl");
        await Assert.That(generated).Contains(": base(default(global::ExternalLib.WrappedClient.State)!)");
    }

    [Test]
    public Task Constructor_Emission_And_Overload_Visibility_Snapshot()
    {
        var source = """
            using TUnit.Mocks;

            public class SnapshotClient
            {
                protected SnapshotClient() { }

                protected SnapshotClient(ProtectedState state) { }

                protected SnapshotClient(InternalState state) { }

                protected SnapshotClient(PublicState state) { }

                protected class ProtectedState { }

                protected internal class InternalState { }

                public class PublicState { }
            }

            public class Test
            {
                public void Run() => Mock.Of<SnapshotClient>();
            }
            """;

        return VerifyGeneratorOutput(source);
    }

    private static async Task<string> GenerateFromExternalLibrary(string externalLibrary, string typeName)
    {
        var reference = CreateExternalAssemblyReference(externalLibrary);
        var source = MockSource(typeName);

        await AssertNoAccessibilityErrors(source, reference);
        return string.Join(Environment.NewLine, RunGenerator(source, [reference]));
    }

    private static Task AssertNoAccessibilityErrors(
        string source,
        MetadataReference? reference = null)
        => AssertNoCompilerErrors(source, reference, "CS0051", "CS0122");

    private static async Task AssertNoCompilerErrors(
        string source,
        MetadataReference? reference,
        params string[] diagnosticIds)
    {
        MetadataReference[]? references = reference is null ? null : [reference];
        var errors = GetGeneratedCompilationErrors(source, references)
            .Where(diagnostic => diagnosticIds.Contains(diagnostic.Id, StringComparer.Ordinal))
            .Select(diagnostic => diagnostic.ToString())
            .ToList();

        await Assert.That(errors).IsEmpty();
    }

    private static string GetMemberSurface(string[] sources)
        => sources.Single(source => source.Contains("_MockMemberExtensions", StringComparison.Ordinal));

    private static string MockSource(string typeName) => $$"""
        using TUnit.Mocks;

        public class Test
        {
            public void Run() => Mock.Of<{{typeName}}>();
        }
        """;

    private static string GrpcMockSource() => """
        using TUnit.Mocks;

        public class Test
        {
            public void Run()
            {
                var mock = Mock.Of<ExternalLib.GrpcClient>();
                mock.Call().Returns("mocked");
                _ = mock.Object.Clone();
            }
        }
        """;
}
