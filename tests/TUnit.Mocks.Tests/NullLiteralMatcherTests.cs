namespace TUnit.Mocks.Tests;

public interface INullLiteralService
{
    string Five(string? text, int number, int? nullable, string? other, int last);
    string Take(string? value);
    string Pair(string? first, string? second);
    void Notify(string? value);
    string NullableNumber(int? value);
    string Number(int value);
    string GenericNumber<T>(T value);
}

public class NullLiteralMatcherTests
{
    [Test]
    public async Task Default_Literals_Without_Predicate_Overloads_Match_Only_Default_Values()
    {
        var mock = Mock.Of<INullLiteralService>();
        mock.Five(default, default, default, "exact", 4).Returns("matched");

        await Assert.That(mock.Object.Five(null, 0, null, "exact", 4)).IsEqualTo("matched");
        await Assert.That(mock.Object.Five("value", 0, null, "exact", 4)).IsEqualTo("");
        await Assert.That(mock.Object.Five(null, 1, null, "exact", 4)).IsEqualTo("");
        await Assert.That(mock.Object.Five(null, 0, 1, "exact", 4)).IsEqualTo("");
        await Assert.That(mock.Object.Five(null, 0, null, "other", 4)).IsEqualTo("");

        mock.Five(default, default, default, "exact", 4).WasCalled(Times.Once);
        mock.Five(default, default, default, "missing", 4).WasNeverCalled();
    }

    [Test]
    public async Task Default_Argument_Has_Empty_Capture_State()
    {
        Arguments.Arg<int> argument = default;

        await Assert.That(argument.Values.Count).IsEqualTo(0);
        await Assert.That(argument.Latest).IsEqualTo(0);
        await Assert.That(argument.Matcher.Matches(0)).IsTrue();
        await Assert.That(argument.Matcher.Matches(1)).IsFalse();
    }

    [Test]
    public async Task Default_Literal_For_NonNullable_Number_Matches_Zero()
    {
        var mock = Mock.Of<INullLiteralService>();
        mock.Number(default).Returns("zero");

        await Assert.That(mock.Object.Number(0)).IsEqualTo("zero");
        await Assert.That(mock.Object.Number(1)).IsEqualTo("");

        mock.Number(default).WasCalled(Times.Once);
        mock.Number(default(int)).WasCalled(Times.Once);
    }

    [Test]
    public async Task Default_Literal_For_Generic_Parameter_Matches_Its_Default_Value()
    {
        var mock = Mock.Of<INullLiteralService>();
        mock.GenericNumber<int>(default).Returns("zero");
        mock.GenericNumber<string?>(default).Returns("null");

        await Assert.That(mock.Object.GenericNumber(0)).IsEqualTo("zero");
        await Assert.That(mock.Object.GenericNumber(1)).IsEqualTo("");
        await Assert.That(mock.Object.GenericNumber<string?>(null)).IsEqualTo("null");
        await Assert.That(mock.Object.GenericNumber<string?>("value")).IsEqualTo("");

        mock.GenericNumber<int>(default).WasCalled(Times.Once);
        mock.GenericNumber<string?>(default).WasCalled(Times.Once);
    }

    [Test]
    public async Task Null_And_Default_Literals_Match_Only_Null()
    {
        var mock = Mock.Of<INullLiteralService>();
        mock.Take(null).Returns("null");
        mock.NullableNumber(default).Returns("null number");
        mock.Notify(default).WasNeverCalled();

        await Assert.That(mock.Object.Take(null)).IsEqualTo("null");
        await Assert.That(mock.Object.Take("value")).IsEqualTo("");
        await Assert.That(mock.Object.NullableNumber(null)).IsEqualTo("null number");
        await Assert.That(mock.Object.NullableNumber(1)).IsEqualTo("");
        mock.Object.Notify(null);
        mock.Object.Notify("value");

        mock.Take(default).WasCalled(Times.Once);
        mock.NullableNumber(null).WasCalled(Times.Once);
        mock.Notify(null).WasCalled(Times.Once);
    }

    [Test]
    public async Task Null_Literals_Combine_With_Predicates_And_Exact_Values()
    {
        var mock = Mock.Of<INullLiteralService>();
        mock.Pair(null, value => value == "second").Returns("first null");
        mock.Pair(value => value == "first", default).Returns("second null");
        mock.Pair(null, "exact").Returns("exact");
        mock.Pair(null, null).Returns("both null");

        await Assert.That(mock.Object.Pair(null, "second")).IsEqualTo("first null");
        await Assert.That(mock.Object.Pair("first", null)).IsEqualTo("second null");
        await Assert.That(mock.Object.Pair(null, "exact")).IsEqualTo("exact");
        await Assert.That(mock.Object.Pair(null, null)).IsEqualTo("both null");
        await Assert.That(mock.Object.Pair("other", "second")).IsEqualTo("");
        mock.Pair(null, value => value == "second").WasCalled(Times.Once);
        mock.Pair(value => value == "first", default).WasCalled(Times.Once);
        mock.Pair(default, default).WasCalled(Times.Once);
    }
}
