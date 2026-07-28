namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Regression: https://github.com/thomhurst/TUnit/issues/6491
/// A public interface may declare <c>internal</c> abstract members (C# 8+). No type outside the
/// declaring assembly can implement it, so it is not mockable. Auto-mock discovery used to walk
/// into such interfaces anyway when they appeared as a member return type — mocking the outer type
/// (RavenDB's <c>IDocumentStore</c> in the report) then failed with CS0535/CS0548/CS0551 on
/// generated code for an interface the user never asked to mock.
/// </summary>
public class Issue6491Tests : SnapshotTestBase
{
    private const string ExternalLibrary = """
        namespace ExternalLib
        {
            public interface IInternalMemberInterface
            {
                internal string MissingProperties { get; set; }

                string Describe();
            }

            public interface IPublicConverter
            {
                string Convert(string value);
            }

            public interface IDocumentStore
            {
                IInternalMemberInterface Converter { get; }

                IPublicConverter PublicConverter { get; }

                IInternalMemberInterface GetConverter();
            }

            // protected / protected internal members are reachable from any assembly through
            // explicit interface implementation, so these stay mockable.
            public interface IProtectedMemberInterface
            {
                protected string Hidden { get; set; }

                protected internal string AlsoHidden { get; }

                string Describe();
            }
        }
        """;

    [Test]
    public async Task Mocking_Type_Reaching_An_Unimplementable_Interface_Emits_No_Unimplemented_Member_Errors()
    {
        var source = """
            using TUnit.Mocks;

            public class Test
            {
                public void Run()
                {
                    var store = Mock.Of<ExternalLib.IDocumentStore>();
                }
            }
            """;

        var errors = GetGeneratedCompilationErrors(source, [CreateExternalAssemblyReference(ExternalLibrary)]);

        // The Roslyn behind this harness predates C# 14 extension blocks, so the generated
        // extension surface always produces parse errors here. Assert on the codes the issue
        // reported instead: unimplemented / accessor-less interface members.
        var interfaceImplementationErrors = errors
            .Where(e => e.Id is "CS0535" or "CS0548" or "CS0551" or "CS0122")
            .Select(e => e.ToString())
            .ToList();

        await Assert.That(interfaceImplementationErrors).IsEmpty();
    }

    [Test]
    public async Task Unimplementable_Interface_Gets_No_Generated_Mock()
    {
        var source = """
            using TUnit.Mocks;

            public class Test
            {
                public void Run()
                {
                    var store = Mock.Of<ExternalLib.IDocumentStore>();
                }
            }
            """;

        var generated = RunGenerator(source, [CreateExternalAssemblyReference(ExternalLibrary)]);

        // The unimplementable interface is skipped entirely...
        await Assert.That(generated.Any(g => g.Contains("IInternalMemberInterfaceMockImpl"))).IsFalse();
        // ...while sibling interfaces that *are* implementable still get their auto-mock factory.
        await Assert.That(generated.Any(g => g.Contains("IPublicConverterMockImpl"))).IsTrue();
    }

    [Test]
    public async Task Interface_With_Protected_Members_Is_Still_Mockable()
    {
        // A protected (or protected internal) interface member is implementable from any assembly
        // through explicit interface implementation, so it must not be mistaken for an
        // assembly-gated one. Compilation.IsSymbolAccessibleWithin(member, assembly) reports these
        // as inaccessible, which would silently drop the interface from generation.
        var source = """
            using TUnit.Mocks;

            public class Test
            {
                public void Run()
                {
                    var protectedMembers = Mock.Of<ExternalLib.IProtectedMemberInterface>();
                }
            }
            """;

        var generated = RunGenerator(source, [CreateExternalAssemblyReference(ExternalLibrary)]);

        await Assert.That(generated.Any(g => g.Contains("IProtectedMemberInterfaceMockImpl"))).IsTrue();
    }
}
