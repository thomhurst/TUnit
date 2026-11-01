using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7316;

/// <summary>
/// Regression fixture for issue #7316: a parameter type with an [Obsolete(error: true)]
/// conversion operator broke the build (CS0619 in the generated AOT converters). The
/// generator must skip that operator and still convert through the usable one.
/// https://github.com/thomhurst/TUnit/issues/7316
/// </summary>
public readonly record struct Token(string Value)
{
    public static implicit operator Token(string value) => new(value);

    [Obsolete("Use a string.", error: true)]
    public static implicit operator Token(char value) => new(value.ToString());
}

[EngineTest(ExpectedResult.Pass)]
public class Tests
{
    public static IEnumerable<(Token Token, int Length)> TupleData()
    {
        yield return ("abc", 3);
    }

    [Test]
    [Arguments("abc")]
    public async Task Converts_Through_Usable_Operator(Token token)
    {
        await Assert.That(token.Value).IsEqualTo("abc");
    }

    [Test]
    [MethodDataSource(nameof(TupleData))]
    public async Task Converts_Tuple_Element((Token Token, int Length) value)
    {
        await Assert.That(value.Token.Value.Length).IsEqualTo(value.Length);
    }
}
