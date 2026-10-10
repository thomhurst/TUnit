using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

public class MatcherBaseEvent;

public class MatcherDerivedEvent : MatcherBaseEvent;

public class MatcherOtherEvent : MatcherBaseEvent;

public interface IMatcherHandler
{
    string Handle(MatcherBaseEvent evt);
    string Echo(string text);
    string Lookup(object key);
}

public class StringAndTypeMatcherTests
{
    [Test]
    public async Task StartsWith_Matches_Prefix_Only()
    {
        var mock = IMatcherHandler.Mock();
        mock.Echo(StartsWith("user_")).Returns("hit");
        var handler = mock.Object;

        await Assert.That(handler.Echo("user_42")).IsEqualTo("hit");
        await Assert.That(handler.Echo("admin_user_")).IsEqualTo("");
        await Assert.That(handler.Echo(null!)).IsEqualTo("");
    }

    [Test]
    public async Task EndsWith_Matches_Suffix_Only()
    {
        var mock = IMatcherHandler.Mock();
        mock.Echo(EndsWith(".json")).Returns("hit");
        var handler = mock.Object;

        await Assert.That(handler.Echo("data.json")).IsEqualTo("hit");
        await Assert.That(handler.Echo("data.json.bak")).IsEqualTo("");
    }

    [Test]
    public async Task Contains_String_Matches_Substring()
    {
        var mock = IMatcherHandler.Mock();
        mock.Echo(Contains("needle")).Returns("hit");
        var handler = mock.Object;

        await Assert.That(handler.Echo("hay needle hay")).IsEqualTo("hit");
        await Assert.That(handler.Echo("hay")).IsEqualTo("");
    }

    [Test]
    public async Task String_Matchers_Honor_StringComparison()
    {
        var mock = IMatcherHandler.Mock();
        mock.Echo(StartsWith("HELLO", StringComparison.OrdinalIgnoreCase)).Returns("a");
        mock.Echo(EndsWith("WORLD", StringComparison.OrdinalIgnoreCase)).Returns("b");
        mock.Echo(Contains("MID", StringComparison.OrdinalIgnoreCase)).Returns("c");
        var handler = mock.Object;

        await Assert.That(handler.Echo("hello there")).IsEqualTo("a");
        await Assert.That(handler.Echo("big world")).IsEqualTo("b");
        await Assert.That(handler.Echo("a mid b")).IsEqualTo("c");
    }

    [Test]
    public async Task String_Matchers_Are_Case_Sensitive_By_Default()
    {
        var mock = IMatcherHandler.Mock();
        mock.Echo(StartsWith("Hello")).Returns("hit");

        await Assert.That(mock.Object.Echo("hello")).IsEqualTo("");
    }

    [Test]
    public async Task String_Matchers_Work_In_Verification_And_Capture()
    {
        var mock = IMatcherHandler.Mock();
        var arg = StartsWith("a");
        mock.Echo(arg).Returns("x");
        var handler = mock.Object;

        handler.Echo("apple");
        handler.Echo("banana");
        handler.Echo("avocado");

        mock.Echo(StartsWith("a")).WasCalled(Times.Exactly(2));
        mock.Echo(Contains("nan")).WasCalled(Times.Once);
        await Assert.That(arg.Values).IsEquivalentTo(new[] { "apple", "avocado" });
    }

    [Test]
    public async Task String_Matcher_Null_Argument_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => StartsWith(null!));
        Assert.Throws<ArgumentNullException>(() => EndsWith(null!));
        Assert.Throws<ArgumentNullException>(() => Contains((string)null!));
        await Task.CompletedTask;
    }

    [Test]
    public async Task IsOfType_Matches_Derived_Types_For_Base_Typed_Parameter()
    {
        var mock = IMatcherHandler.Mock();
        mock.Handle(IsOfType<MatcherDerivedEvent>()).Returns("derived");
        var handler = mock.Object;

        await Assert.That(handler.Handle(new MatcherDerivedEvent())).IsEqualTo("derived");
        await Assert.That(handler.Handle(new MatcherOtherEvent())).IsEqualTo("");
        await Assert.That(handler.Handle(new MatcherBaseEvent())).IsEqualTo("");
        await Assert.That(handler.Handle(null!)).IsEqualTo("");
    }

    [Test]
    public async Task IsOfType_Distinguishes_Between_Subtypes_In_Separate_Setups()
    {
        var mock = IMatcherHandler.Mock();
        mock.Handle(IsOfType<MatcherDerivedEvent>()).Returns("derived");
        mock.Handle(IsOfType<MatcherOtherEvent>()).Returns("other");
        var handler = mock.Object;

        await Assert.That(handler.Handle(new MatcherOtherEvent())).IsEqualTo("other");
        await Assert.That(handler.Handle(new MatcherDerivedEvent())).IsEqualTo("derived");
    }

    [Test]
    public async Task IsOfType_Works_In_Verification()
    {
        var mock = IMatcherHandler.Mock();
        var handler = mock.Object;

        handler.Handle(new MatcherDerivedEvent());
        handler.Handle(new MatcherOtherEvent());
        handler.Handle(new MatcherDerivedEvent());

        mock.Handle(IsOfType<MatcherDerivedEvent>()).WasCalled(Times.Exactly(2));
        mock.Handle(IsOfType<MatcherBaseEvent>()).WasCalled(Times.Exactly(3));
        await Task.CompletedTask;
    }

    [Test]
    public async Task IsOfType_Works_For_Object_Parameters()
    {
        var mock = IMatcherHandler.Mock();
        mock.Lookup(IsOfType<string>()).Returns("string-key");
        var handler = mock.Object;

        await Assert.That(handler.Lookup("a")).IsEqualTo("string-key");
        await Assert.That(handler.Lookup(5)).IsEqualTo("");
    }

    [Test]
    public async Task IsSameAs_Uses_Reference_Equality()
    {
        var instance = new EqualsAlwaysTrue();
        var mock = IMatcherHandler.Mock();
        mock.Lookup(IsSameAs<object>(instance)).Returns("same");
        var handler = mock.Object;

        await Assert.That(handler.Lookup(instance)).IsEqualTo("same");
        await Assert.That(handler.Lookup(new EqualsAlwaysTrue())).IsEqualTo("");
        await Assert.That(handler.Lookup(null!)).IsEqualTo("");
    }

    [Test]
    public async Task IsSameAs_Null_Reference_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => IsSameAs<object>(null!));
        await Task.CompletedTask;
    }

    [Test]
    public async Task Describe_Text_Appears_In_Verification_Failure()
    {
        var mock = IMatcherHandler.Mock();
        mock.Object.Echo("zzz");

        var ex = Assert.Throws<TUnit.Mocks.Exceptions.MockVerificationException>(
            () => mock.Echo(StartsWith("abc")).WasCalled(Times.Once));

        await Assert.That(ex.Message).Contains("StartsWith(\"abc\")");
    }

    private sealed class EqualsAlwaysTrue
    {
        public override bool Equals(object? obj) => obj is EqualsAlwaysTrue;

        public override int GetHashCode() => 1;
    }
}
