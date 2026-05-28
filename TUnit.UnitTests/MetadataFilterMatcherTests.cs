using System.Reflection;
using Microsoft.Testing.Platform.Extensions.Messages;
using Microsoft.Testing.Platform.Requests;
using TUnit.Engine.Services;

namespace TUnit.UnitTests;

/// <summary>
/// Direct unit tests for <see cref="MetadataFilterMatcher.ExtractFilterHints"/>.
/// Regression coverage for GitHub issue #6026: TreeNodeFilter operator characters
/// (parens, |, &amp;, !, escape \) in any path segment must be treated as
/// non-literal so the source-gen pre-filter does not exclude valid descriptors
/// before MTP's authoritative TreeNodeFilter runs.
/// </summary>
public class MetadataFilterMatcherTests
{
#pragma warning disable TPEXP
    private static readonly ConstructorInfo? TreeNodeFilterCtor =
        typeof(TreeNodeFilter).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance, null, [typeof(string)], null);

    private static TreeNodeFilter CreateFilter(string pattern)
    {
        var ctor = TreeNodeFilterCtor
            ?? throw new InvalidOperationException(
                "TreeNodeFilter(string) non-public ctor not found — has Microsoft.Testing.Platform changed its API?");
        return (TreeNodeFilter)ctor.Invoke([pattern]);
    }
#pragma warning restore TPEXP

    [Test]
    public async Task PlainLiteralSegments_ExtractAllFourHints()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/MyAsm/MyNs/MyClass/MyMethod"));

        await Assert.That(hints.AssemblyName).IsEqualTo("MyAsm");
        await Assert.That(hints.Namespace).IsEqualTo("MyNs");
        await Assert.That(hints.ClassName).IsEqualTo("MyClass");
        await Assert.That(hints.MethodName).IsEqualTo("MyMethod");
        await Assert.That(hints.HasHints).IsTrue();
    }

    [Test]
    public async Task DottedNamespace_StaysLiteral()
    {
        // '.' is not an MTP operator — dotted namespaces must remain literal hints.
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/MyAsm/Foo.Bar.Baz/MyClass/MyMethod"));

        await Assert.That(hints.Namespace).IsEqualTo("Foo.Bar.Baz");
    }

    [Test]
    public async Task NestedClassSegment_StaysLiteral()
    {
        // '+' is not an MTP operator — nested class hierarchies must remain literal.
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/MyAsm/MyNs/Outer+Inner/MyMethod"));

        await Assert.That(hints.ClassName).IsEqualTo("Outer+Inner");
    }

    [Test]
    public async Task WildcardStar_SkipsHint()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/*/*/*/MyMethod"));

        await Assert.That(hints.AssemblyName).IsNull();
        await Assert.That(hints.Namespace).IsNull();
        await Assert.That(hints.ClassName).IsNull();
        await Assert.That(hints.MethodName).IsEqualTo("MyMethod");
    }

    [Test]
    public async Task EmbeddedWildcard_SkipsHint()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/*/*/MyClass*/MyMethod"));

        await Assert.That(hints.ClassName).IsNull();
        await Assert.That(hints.MethodName).IsEqualTo("MyMethod");
    }

    [Test]
    public async Task QuestionMarkWildcard_SkipsHint()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/*/*/My?lass/MyMethod"));

        await Assert.That(hints.ClassName).IsNull();
    }

    [Test]
    public async Task ParenthesisedMethod_SkipsMethodHint()
    {
        // The exact repro from #6026 — paren-wrapped method name must NOT become a literal.
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/*/*/*/(MyTest1)"));

        await Assert.That(hints.MethodName).IsNull();
        await Assert.That(hints.HasHints).IsFalse();
    }

    [Test]
    public async Task OrExpressionMethod_SkipsMethodHint()
    {
        // Second pattern from #6026 — pipe-separated alternation.
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/*/*/*/(MyTest1|MyTest2)"));

        await Assert.That(hints.MethodName).IsNull();
        await Assert.That(hints.HasHints).IsFalse();
    }

    [Test]
    public async Task AndExpressionMethod_SkipsMethodHint()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/*/*/*/(MyTest1&MyTest2)"));

        await Assert.That(hints.MethodName).IsNull();
    }

    [Test]
    public async Task NotExpressionMethod_SkipsMethodHint()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/*/*/*/!MyTest1"));

        await Assert.That(hints.MethodName).IsNull();
    }

    [Test]
    public async Task EscapeCharacter_SkipsHint()
    {
        // '\.' escapes a literal dot in TreeNodeFilter grammar; stored as-is it would
        // never equal the actual method name. Skip the hint and let MTP unescape.
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter(@"/*/*/MyClass/Foo\.Bar"));

        await Assert.That(hints.MethodName).IsNull();
    }

    [Test]
    public async Task ParenthesisedClassSegment_SkipsClassHint()
    {
        // Operator chars in any segment skip only that segment's hint.
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/MyAsm/MyNs/(ClassA|ClassB)/MyMethod"));

        await Assert.That(hints.AssemblyName).IsEqualTo("MyAsm");
        await Assert.That(hints.Namespace).IsEqualTo("MyNs");
        await Assert.That(hints.ClassName).IsNull();
        await Assert.That(hints.MethodName).IsEqualTo("MyMethod");
    }

    [Test]
    public async Task ParenthesisedNamespaceSegment_SkipsNamespaceHint()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/MyAsm/(NsA|NsB)/MyClass/MyMethod"));

        await Assert.That(hints.AssemblyName).IsEqualTo("MyAsm");
        await Assert.That(hints.Namespace).IsNull();
        await Assert.That(hints.ClassName).IsEqualTo("MyClass");
        await Assert.That(hints.MethodName).IsEqualTo("MyMethod");
    }

    [Test]
    public async Task PropertyBagBrackets_StrippedBeforeHintExtraction()
    {
        // [key=value] is stripped before segments are inspected, so the surrounding
        // literal text still becomes a hint.
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter("/MyAsm/MyNs/MyClass/MyMethod[Category=Smoke]"));

        await Assert.That(hints.MethodName).IsEqualTo("MyMethod");
    }

    [Test]
    public async Task NullFilter_NoHints()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(filter: null);

        await Assert.That(hints.HasHints).IsFalse();
    }

    [Test]
    public async Task EmptyFilterString_NoHints()
    {
        var hints = MetadataFilterMatcher.ExtractFilterHints(CreateFilter(string.Empty));

        await Assert.That(hints.HasHints).IsFalse();
    }
}
