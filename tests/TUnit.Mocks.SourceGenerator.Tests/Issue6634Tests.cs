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

    private static async Task AssertNoAccessibilityErrors(
        string source,
        MetadataReference? reference = null)
    {
        MetadataReference[]? references = reference is null ? null : [reference];
        var errors = GetGeneratedCompilationErrors(source, references)
            .Where(diagnostic => diagnostic.Id is "CS0051" or "CS0122")
            .Select(diagnostic => diagnostic.ToString())
            .ToList();

        await Assert.That(errors).IsEmpty();
    }

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
