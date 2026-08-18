using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Regression: https://github.com/thomhurst/TUnit/issues/6634
/// Constructor discovery runs in the generated subclass context, where protected nested types are
/// accessible. Constructor models also feed a non-derived factory and static extension methods,
/// whose signatures cannot name those types unless the consumer assembly has internal access.
/// </summary>
public class Issue6634Tests : SnapshotTestBase
{
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
        var references = reference is null ? null : new[] { reference };
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
}
