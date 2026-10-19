using TUnit.Core.Helpers;

namespace TUnit.UnitTests;

public class ArgumentFormatterTests
{
    [Test]
    public async Task FormatDefault_ValueTuple_FormatsCorrectly()
    {
        var tuple = (1, "test", true);

        var result = ArgumentFormatter.Format(tuple, []);

        await Assert.That(result).IsEqualTo("(1, test, True)");
    }

    [Test]
    public async Task FormatDefault_NestedValueTuple_FormatsCorrectly()
    {
        var tuple = (1, (2, "inner"), true);

        var result = ArgumentFormatter.Format(tuple, []);

        await Assert.That(result).IsEqualTo("(1, (2, inner), True)");
    }

    [Test]
    public async Task FormatDefault_SystemTuple_FormatsCorrectly()
    {
        var tuple = Tuple.Create(1, "test", false);

        var result = ArgumentFormatter.Format(tuple, []);

        await Assert.That(result).IsEqualTo("(1, test, False)");
    }

    [Test]
    public async Task FormatArguments_WithTuples_FormatsCorrectly()
    {
        var args = new object?[] { (1, "a"), 42, (true, 3.14) };

        var result = ArgumentFormatter.FormatArguments(args);

        // Primitives are formatted with ToString(), i.e. the current culture (3,14 under sv-SE).
        await Assert.That(result).IsEqualTo($"(1, a), 42, (True, {3.14})");
    }

    [Test]
    public async Task FormatDefault_LargeTuple_FormatsAllElements()
    {
        var tuple = (1, 2, 3, 4, 5, 6, 7, 8);

        var result = ArgumentFormatter.Format(tuple, []);

        await Assert.That(result).IsEqualTo("(1, 2, 3, 4, 5, 6, 7, 8)");
    }

    [Test]
    public async Task FormatDefault_TupleWithNull_HandlesNullCorrectly()
    {
        (int, string?, string) tuple = (1, null, "test");

        var result = ArgumentFormatter.Format(tuple, []);

        await Assert.That(result).IsEqualTo("(1, null, test)");
    }

    [Test]
    public async Task FormatDefault_StringWithDots_EscapesDotsWithMiddleDot()
    {
        var stringWithDots = "1.2.3";

        var result = ArgumentFormatter.Format(stringWithDots, []);

        await Assert.That(result).IsEqualTo("1·2·3");
    }

    [Test]
    public async Task FormatDefault_StringWithoutDots_ReturnsUnchanged()
    {
        var stringWithoutDots = "hello world";

        var result = ArgumentFormatter.Format(stringWithoutDots, []);

        await Assert.That(result).IsEqualTo("hello world");
    }

    [Test]
    public async Task FormatArguments_WithStringsContainingDots_EscapesDots()
    {
        var args = new object?[] { "hello", "with.dot", "1.2.3" };

        var result = ArgumentFormatter.FormatArguments(args);

        await Assert.That(result).IsEqualTo("hello, with·dot, 1·2·3");
    }
}