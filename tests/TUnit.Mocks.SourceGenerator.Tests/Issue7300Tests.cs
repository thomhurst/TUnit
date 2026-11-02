using Microsoft.CodeAnalysis;

namespace TUnit.Mocks.SourceGenerator.Tests;

// Regression: https://github.com/thomhurst/TUnit/issues/7300
// A type from a reference that only has an extern alias (<Aliases>LibAlias</Aliases>) is not
// reachable through `global::`, so the generated `global::Lib.Services.IPublisher` failed with
// CS0234. Generated names must use the reference's alias, and each generated file that uses one
// must declare it with `extern alias`.
public class Issue7300Tests : SnapshotTestBase
{
    private const string LibrarySource = """
        using System;
        using System.Threading;
        using System.Threading.Tasks;

        namespace Lib.Services
        {
            public record Message(Guid Id);

            public interface IPublisher
            {
                Task PublishAsync(Guid id, CancellationToken ct);
                Message? Last { get; }
                event EventHandler<Message>? Published;
            }

            public interface IRepository<T>
            {
                T Get(int id);
            }

            public abstract class Handler
            {
                public abstract Message Handle(Message message);
            }
        }
        """;

    private const string Source = """
        extern alias LibAlias;

        using TUnit.Mocks;
        using IPublisher = LibAlias::Lib.Services.IPublisher;
        using Handler = LibAlias::Lib.Services.Handler;
        using Message = LibAlias::Lib.Services.Message;

        [assembly: GenerateMock(typeof(LibAlias::Lib.Services.IRepository<string>))]

        namespace TestNamespace
        {
            public interface ILocal
            {
                Message Echo(Message message);
            }

            public class Usage
            {
                void M()
                {
                    _ = Mock.Of<IPublisher>();
                    _ = Mock.Of<Handler>();
                    _ = Mock.Of<ILocal>();
                    _ = Mock.Of<LibAlias::Lib.Services.IRepository<Message>>();
                }
            }
        }
        """;

    private static MetadataReference[] AliasedLibrary()
        => [CreateExternalAssemblyReference(LibrarySource, "Lib").WithAliases(["LibAlias"])];

    [Test]
    public Task Extern_Aliased_Types_Snapshot()
        => VerifyGeneratorOutput(Source, AliasedLibrary());

    [Test]
    public async Task Keyword_Alias_Produces_Equivalent_Generated_Code()
    {
        var expected = RunGenerator(Source, AliasedLibrary());
        var actual = RunGenerator(Source.Replace("LibAlias", "@class"),
            [CreateExternalAssemblyReference(LibrarySource, "Lib").WithAliases(["class"])])
            .Select(source => source.Replace("@class", "LibAlias"))
            .ToArray();

        // Includes declarations, qualified type names, and generated names: escaping the alias
        // must not change the mock's identity or its closed-generic factory registrations.
        await Assert.That(actual).IsEquivalentTo(expected);
    }

    [Test]
    public async Task Extern_Aliased_Types_Resolve_In_Generated_Code()
    {
        var errors = GetGeneratedCompilationErrors(Source, AliasedLibrary());

        // The Roslyn behind this harness predates C# 14 extension blocks, so the generated
        // extension surface always produces parse errors here, and the type parameter of an
        // unparsed `extension<T>` block is reported as missing (CS0246 on 'T'). Assert on name
        // resolution and extern alias errors instead: CS0234/CS0246 (the issue), CS0430/CS0432
        // (an undeclared or unknown alias), CS0439 (an extern alias after other declarations).
        var resolutionErrors = errors
            .Where(e => e.Id is "CS0234" or "CS0246" or "CS0430" or "CS0432" or "CS0439")
            .Where(e => !e.GetMessage(System.Globalization.CultureInfo.InvariantCulture).Contains("'T'"))
            .Select(e => e.ToString())
            .ToList();

        await Assert.That(resolutionErrors).IsEmpty();
    }
}
