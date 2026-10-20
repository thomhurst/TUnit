using System.Text.RegularExpressions;
using TUnit.Assertions.SourceGenerator.Generators;

namespace TUnit.Assertions.SourceGenerator.Tests;

/// <summary>
/// Pins how <see cref="AssertionMethodGenerator"/> names condition classes that would otherwise share a name: each one
/// binds to its own class, and a name depends only on the declarations it is generated from, never on the order
/// attributes are declared or emitted in.
/// </summary>
internal class ConditionClassNamingTests
{
    private static readonly Regex ClassDeclarationRegex = new(@"public class (?<name>\w+)");

    private const string Overloads =
        """
        public static bool Matches(string value, int count) => true;
        public static bool Matches(string value, bool flag) => true;
        """;

    [Test]
    public async Task Overloads_Sharing_A_Name_Each_Bind_To_Their_Own_Class()
    {
        var source = OverloadsSource(Overloads);
        var (sources, diagnostics) = AssertionMethodGeneratorRunner.Run(source);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(ClassNames(sources)).Count().IsEqualTo(2);
        await Assert.That(ClassNames(sources)).Contains("StringOverloadsMatchesWithStringAnd1MoreAssertion");
        await CompileChecker.AssertNoErrors(sources.Append(source));
    }

    [Test]
    public async Task Overload_Names_Do_Not_Depend_On_Declaration_Order()
    {
        var reordered = string.Join(Environment.NewLine, Overloads.Split('\n').Select(line => line.TrimEnd('\r')).Reverse());

        var names = ClassNamesByConstructor(AssertionMethodGeneratorRunner.Run(OverloadsSource(Overloads)).Sources);
        var reorderedNames = ClassNamesByConstructor(AssertionMethodGeneratorRunner.Run(OverloadsSource(reordered)).Sources);

        await Assert.That(reorderedNames).IsEquivalentTo(names);
    }

    [Test]
    public async Task Overloads_With_Unusual_Parameter_Types_Each_Get_Their_Own_Class()
    {
        var source = OverloadsSource(
            """
            public static bool Matches(string value, int[] values) => true;
            public static bool Matches(string value, int? count) => true;
            public static bool Matches(string value, System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<int>> map) => true;
            public static bool Matches<T>(string value, T item) => true;
            """);

        var (sources, _) = AssertionMethodGeneratorRunner.Run(source);

        await Assert.That(ClassNames(sources).Distinct()).Count().IsEqualTo(4);
        await CompileChecker.AssertNoErrors(sources.Append(source));
    }

    [Test]
    public async Task Attributes_With_Different_Messages_For_One_Method_Each_Keep_Their_Message()
    {
        var source = MessagesSource(first: "be first", second: "be second");
        var (sources, diagnostics) = AssertionMethodGeneratorRunner.Run(source);
        var expectations = AssertionMethodGeneratorRunner.Expectations(sources);

        await Assert.That(diagnostics).IsEmpty();
        await Assert.That(expectations).Contains(@"$""to {(_negated ? ""not "" : """")}be first""");
        await Assert.That(expectations).Contains(@"$""to {(_negated ? ""not "" : """")}be second""");
        await CompileChecker.AssertNoErrors(sources.Append(source));
    }

    [Test]
    public async Task Names_Do_Not_Depend_On_The_Order_Attributes_Are_Declared_In()
    {
        var names = ClassNamesByExpectation(MessagesSource(first: "be first", second: "be second"));
        var swappedNames = ClassNamesByExpectation(MessagesSource(first: "be second", second: "be first"));

        await Assert.That(swappedNames).IsEquivalentTo(names);
    }

    [Test]
    public async Task Names_Do_Not_Change_When_An_Unrelated_Attribute_Is_Added()
    {
        var source = MessagesSource(first: "be first", second: "be second");
        var withUnrelated = source.Replace(
            "public static partial class FirstAssertions { }",
            """
            [AssertionFrom<string>(typeof(Methods), nameof(Methods.Unrelated), ExpectationMessage = "be unrelated")]
            public static partial class FirstAssertions { }
            """);

        var names = ClassNamesByExpectation(source);
        var namesWithUnrelated = ClassNamesByExpectation(withUnrelated);

        await Assert.That(namesWithUnrelated.Where(name => !name.Contains("Unrelated"))).IsEquivalentTo(names);
    }

    private static string OverloadsSource(string overloads) =>
        $$"""
        using TUnit.Assertions.Attributes;

        namespace Naming;

        public static class Overloads
        {
        {{overloads}}
        }

        [AssertionFrom<string>(typeof(Overloads), nameof(Overloads.Matches))]
        public static partial class OverloadAssertions { }
        """;

    private static string MessagesSource(string first, string second) =>
        $$"""
        using TUnit.Assertions.Attributes;

        namespace Naming;

        public static class Methods
        {
            public static bool Shared(string value) => true;
            public static bool Unrelated(string value) => true;
        }

        [AssertionFrom<string>(typeof(Methods), nameof(Methods.Shared), CustomName = "IsFirst", ExpectationMessage = "{{first}}")]
        public static partial class FirstAssertions { }

        [AssertionFrom<string>(typeof(Methods), nameof(Methods.Shared), CustomName = "IsSecond", ExpectationMessage = "{{second}}")]
        public static partial class SecondAssertions { }
        """;

    private static string[] ClassNames(IEnumerable<string> sources) =>
        sources
            .SelectMany(source => ClassDeclarationRegex.Matches(source).Cast<Match>())
            .Select(match => match.Groups["name"].Value)
            .ToArray();

    /// <summary>Each condition class's name, paired with the parameters its constructor takes.</summary>
    private static string[] ClassNamesByConstructor(IEnumerable<string> sources) =>
        sources
            .SelectMany(source => Regex.Matches(source, @"public (?<name>\w+)\(AssertionContext<[^>]*> context(?<parameters>[^)]*)\)").Cast<Match>())
            .Select(match => $"{match.Groups["name"].Value}: {match.Groups["parameters"].Value}")
            .ToArray();

    /// <summary>Each condition class's name, paired with the expectation it renders.</summary>
    private static string[] ClassNamesByExpectation(string source)
    {
        var (sources, _) = AssertionMethodGeneratorRunner.Run(source);

        return sources
            .SelectMany(text => Regex.Matches(text, @"public class (?<name>\w+).*?GetExpectation\(\)\s*\{\s*return (?<expectation>[^\r\n]*);", RegexOptions.Singleline).Cast<Match>())
            .Select(match => $"{match.Groups["name"].Value}: {match.Groups["expectation"].Value}")
            .ToArray();
    }
}
