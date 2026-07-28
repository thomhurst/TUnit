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

    [Test]
    public async Task Sanitized_Names_Keep_Separators_And_Underscores_Apart()
    {
        // The realistic shapes must all stay distinct; a run of underscores in the output is only
        // ever a literal underscore when it is doubled.
        await Assert.That(IdentifierEscaping.SanitizeIdentifier("global::A.B.IFoo")).IsEqualTo("A_B_IFoo");
        await Assert.That(IdentifierEscaping.SanitizeIdentifier("global::A_B.IFoo")).IsEqualTo("A__B_IFoo");
        await Assert.That(IdentifierEscaping.SanitizeIdentifier("global::A.B_IFoo")).IsEqualTo("A_B__IFoo");
        await Assert.That(IdentifierEscaping.SanitizeIdentifier("global::A.IFoo<global::A.B>")).IsEqualTo("A_IFoo_A_B_");
    }

    [Test]
    public async Task Names_That_Still_Collide_Are_Reported_Instead_Of_Silently_Dropped()
    {
        // No mapping onto [A-Za-z0-9_] can be injective while both a separator and an underscore
        // render as runs of '_': three underscores cannot say which order they came in, so
        // 'A_.B.IFoo' and 'A._B.IFoo' both sanitize to 'A___B_IFoo'. That must fail loudly.
        var source = """
            using TUnit.Mocks;

            namespace A_.B { public interface IFoo { void Go(); } }
            namespace A._B { public interface IFoo { void Stop(); } }

            public class Test
            {
                public void Run()
                {
                    var one = Mock.Of<A_.B.IFoo>();
                    var two = Mock.Of<A._B.IFoo>();
                }
            }
            """;

        var (sources, diagnostics) = RunGeneratorForDiagnostics(source);

        var collisions = diagnostics.Where(d => d.Id == "TM008").ToList();
        await Assert.That(collisions).HasCount(2);
        await Assert.That(collisions[0].GetMessage()).Contains("A___B_IFoo");
        // Both culprits are named, so the message says what to rename.
        await Assert.That(collisions.Select(d => d.GetMessage()).Any(m => m.Contains("A_.B.IFoo"))).IsTrue();
        await Assert.That(collisions.Select(d => d.GetMessage()).Any(m => m.Contains("A._B.IFoo"))).IsTrue();

        // Nothing was emitted for either type, but the generator did not abort.
        await Assert.That(sources.Any(s => s.Contains("void Go()"))).IsFalse();
        await Assert.That(sources.Any(s => s.Contains("void Stop()"))).IsFalse();
    }

    [Test]
    public async Task A_Collision_Does_Not_Stop_Other_Mocks_In_The_Compilation()
    {
        // The pre-#6505 behaviour was a duplicate hint name aborting the generator, which took
        // every unrelated mock in the compilation with it.
        var source = """
            using TUnit.Mocks;

            namespace A_.B { public interface IFoo { void Go(); } }
            namespace A._B { public interface IFoo { void Stop(); } }

            public interface IUnrelated { void Untouched(); }

            public class Test
            {
                public void Run()
                {
                    var one = Mock.Of<A_.B.IFoo>();
                    var two = Mock.Of<A._B.IFoo>();
                    var three = Mock.Of<IUnrelated>();
                }
            }
            """;

        var (sources, _) = RunGeneratorForDiagnostics(source);

        await Assert.That(sources.Any(s => s.Contains("void Untouched()"))).IsTrue();
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
