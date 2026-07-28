namespace TUnit.Mocks.SourceGenerator.Tests;

/// <summary>
/// Regression: https://github.com/thomhurst/TUnit/issues/6505
/// Generated type names and <c>AddSource</c> hint names come from a sanitized form of the mocked
/// type's fully qualified name. That sanitization used to map every separator to <c>_</c> and
/// collapse runs, so <c>A_B.IFoo</c> and <c>A.B.IFoo</c> both produced <c>A_B_IFoo</c>. Roslyn
/// drops both sources on a duplicate hint name without reporting anything, so mocking both types
/// in one compilation silently generated nothing for either.
/// </summary>
public class Issue6505Tests : SnapshotTestBase
{
    private const string UnderscoreNamespaceInterface = """
        namespace A_B { public interface IFoo { void Go(); } }
        """;

    private const string DottedNamespaceInterface = """
        namespace A.B { public interface IFoo { void Stop(); } }
        """;

    [Test]
    public async Task Types_With_Colliding_Sanitized_Names_Both_Generate()
    {
        var source = $$"""
            using TUnit.Mocks;

            {{UnderscoreNamespaceInterface}}
            {{DottedNamespaceInterface}}

            public class Test
            {
                public void Run()
                {
                    var one = Mock.Of<A_B.IFoo>();
                    var two = Mock.Of<A.B.IFoo>();
                }
            }
            """;

        var generated = RunGenerator(source);

        // Each interface declares a distinct member, so both surfaces are visible in the output.
        await Assert.That(generated.Any(g => g.Contains("void Go()"))).IsTrue();
        await Assert.That(generated.Any(g => g.Contains("void Stop()"))).IsTrue();
    }

    [Test]
    public async Task Mocking_Both_Colliding_Types_Drops_No_Generated_File()
    {
        var underscoreOnly = RunGenerator(MockOf("A_B.IFoo", UnderscoreNamespaceInterface));
        var dottedOnly = RunGenerator(MockOf("A.B.IFoo", DottedNamespaceInterface));
        var both = RunGenerator(MockOf(
            "A_B.IFoo", UnderscoreNamespaceInterface,
            "A.B.IFoo", DottedNamespaceInterface));

        // The post-initialization TUnit.Mocks.Generated namespace stub is the one file all three
        // runs share; everything else must survive being generated alongside the other type.
        await Assert.That(both.Length).IsEqualTo(underscoreOnly.Length + dottedOnly.Length - 1);
    }

    private static string MockOf(string typeName, string declaration) => $$"""
        using TUnit.Mocks;

        {{declaration}}

        public class Test
        {
            public void Run()
            {
                var one = Mock.Of<{{typeName}}>();
            }
        }
        """;

    private static string MockOf(string firstType, string firstDeclaration, string secondType, string secondDeclaration) => $$"""
        using TUnit.Mocks;

        {{firstDeclaration}}
        {{secondDeclaration}}

        public class Test
        {
            public void Run()
            {
                var one = Mock.Of<{{firstType}}>();
                var two = Mock.Of<{{secondType}}>();
            }
        }
        """;
}
