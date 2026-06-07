using TUnit.Assertions.Exceptions;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Tests for <c>MockAssertionExtensions</c> — verifying mock calls through the TUnit assertion
/// pipeline: <c>await Assert.That(mock.Method(...)).WasCalled(...)</c>.
/// Covers positive (assertion passes) and negative (assertion throws) paths across all
/// ICallVerification implementations: method calls, void methods, property getters and setters.
/// </summary>
public class MockAssertionExtensionsTests
{
    public interface IPropertyHolder
    {
        string Name { get; }
        int Count { get; set; }
    }

    // ───────────────────────── WasCalled() shorthand (at least once) ─────────────────────────

    [Test]
    public async Task WasCalled_Shorthand_Passes_When_Called_Once()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        await Assert.That(mock.Add(1, 2)).WasCalled();
    }

    [Test]
    public async Task WasCalled_Shorthand_Passes_When_Called_Multiple_Times()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.Add(1, 2);

        await Assert.That(mock.Add(1, 2)).WasCalled();
    }

    [Test]
    public async Task WasCalled_Shorthand_Fails_When_Never_Called()
    {
        var mock = ICalculator.Mock();

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasCalled());

        await Assert.That(exception.Message).Contains("Mock verification failed");
        await Assert.That(exception.Message).Contains("called 0 time(s)");
    }

    // ───────────────────────── WasCalled(Times) ─────────────────────────

    [Test]
    public async Task WasCalled_Once_Passes_When_Called_Exactly_Once()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        await Assert.That(mock.Add(1, 2)).WasCalled(Times.Once);
    }

    [Test]
    public async Task WasCalled_Once_Fails_When_Never_Called()
    {
        var mock = ICalculator.Mock();

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasCalled(Times.Once));

        await Assert.That(exception.Message).Contains("called 0 time(s)");
    }

    [Test]
    public async Task WasCalled_Once_Fails_When_Called_Twice()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasCalled(Times.Once));

        await Assert.That(exception.Message).Contains("called 2 time(s)");
    }

    [Test]
    public async Task WasCalled_Exactly_Passes()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        await Assert.That(mock.Add(1, 2)).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task WasCalled_Exactly_Fails_When_Count_Wrong()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasCalled(Times.Exactly(3)));

        await Assert.That(exception.Message).Contains("Mock verification failed");
        await Assert.That(exception.Message).Contains("called 1 time(s)");
    }

    [Test]
    public async Task WasCalled_AtLeast_Passes_At_Boundary()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        await Assert.That(mock.Add(1, 2)).WasCalled(Times.AtLeast(2));
    }

    [Test]
    public async Task WasCalled_AtLeast_Fails_Below_Boundary()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasCalled(Times.AtLeast(2)));

        await Assert.That(exception.Message).Contains("called 1 time(s)");
    }

    [Test]
    public async Task WasCalled_AtMost_Passes_At_Boundary()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        await Assert.That(mock.Add(1, 2)).WasCalled(Times.AtMost(2));
    }

    [Test]
    public async Task WasCalled_AtMost_Fails_Above_Boundary()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);
        calc.Add(1, 2);

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasCalled(Times.AtMost(2)));

        await Assert.That(exception.Message).Contains("called 3 time(s)");
    }

    [Test]
    public async Task WasCalled_Between_Passes_Inside_Range()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(1, 2);

        await Assert.That(mock.Add(1, 2)).WasCalled(Times.Between(1, 3));
    }

    [Test]
    public async Task WasCalled_Between_Fails_Outside_Range()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasCalled(Times.Between(2, 4)));

        await Assert.That(exception.Message).Contains("called 1 time(s)");
    }

    [Test]
    public async Task WasCalled_TimesNever_Passes_When_Not_Called()
    {
        var mock = ICalculator.Mock();

        await Assert.That(mock.Add(1, 2)).WasCalled(Times.Never);
    }

    [Test]
    public async Task WasCalled_TimesNever_Fails_When_Called()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasCalled(Times.Never));

        await Assert.That(exception.Message).Contains("called 1 time(s)");
    }

    // ───────────────────────── WasNeverCalled ─────────────────────────

    [Test]
    public async Task WasNeverCalled_Passes_When_Not_Called()
    {
        var mock = ICalculator.Mock();

        await Assert.That(mock.Add(1, 2)).WasNeverCalled();
    }

    [Test]
    public async Task WasNeverCalled_Fails_When_Called()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Add(1, 2)).WasNeverCalled());

        await Assert.That(exception.Message).Contains("Mock verification failed");
        await Assert.That(exception.Message).Contains("called 1 time(s)");
    }

    // ───────────────────────── Argument matching ─────────────────────────

    [Test]
    public async Task WasCalled_Only_Counts_Matching_Arguments()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(3, 4);
        calc.Add(1, 2);

        await Assert.That(mock.Add(1, 2)).WasCalled(Times.Exactly(2));
        await Assert.That(mock.Add(3, 4)).WasCalled(Times.Once);
        await Assert.That(mock.Add(5, 6)).WasNeverCalled();
    }

    [Test]
    public async Task WasCalled_With_Any_Matcher_Counts_All_Calls()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Add(1, 2);
        calc.Add(3, 4);

        await Assert.That(mock.Add(Any(), Any())).WasCalled(Times.Exactly(2));
    }

    // ───────────────────────── Void methods (VoidMockMethodCall) ─────────────────────────

    [Test]
    public async Task Void_Method_WasCalled_Passes()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Log("hello");

        await Assert.That(mock.Log("hello")).WasCalled(Times.Once);
    }

    [Test]
    public async Task Void_Method_WasCalled_Fails_When_Args_Differ()
    {
        var mock = ICalculator.Mock();
        ICalculator calc = mock.Object;
        calc.Log("hello");

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Log("world")).WasCalled(Times.Once));

        await Assert.That(exception.Message).Contains("called 0 time(s)");
    }

    [Test]
    public async Task Void_Method_WasNeverCalled_Passes()
    {
        var mock = ICalculator.Mock();

        await Assert.That(mock.Log("hello")).WasNeverCalled();
    }

    // ───────────────────────── Property getters (PropertyMockCall struct) ─────────────────────────

    [Test]
    public async Task Property_Getter_WasCalled_Passes()
    {
        var mock = IPropertyHolder.Mock();
        mock.Name.Returns("test");
        IPropertyHolder svc = mock.Object;
        _ = svc.Name;
        _ = svc.Name;

        await Assert.That(mock.Name).WasCalled(Times.Exactly(2));
    }

    [Test]
    public async Task Property_Getter_WasCalled_Fails_When_Never_Read()
    {
        var mock = IPropertyHolder.Mock();
        mock.Name.Returns("test");

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Name).WasCalled(Times.Once));

        await Assert.That(exception.Message).Contains("called 0 time(s)");
    }

    [Test]
    public async Task Property_Getter_WasNeverCalled_Passes()
    {
        var mock = IPropertyHolder.Mock();
        mock.Name.Returns("test");

        await Assert.That(mock.Name).WasNeverCalled();
    }

    [Test]
    public async Task Property_Getter_WasNeverCalled_Fails_When_Read()
    {
        var mock = IPropertyHolder.Mock();
        mock.Name.Returns("test");
        IPropertyHolder svc = mock.Object;
        _ = svc.Name;

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Name).WasNeverCalled());

        await Assert.That(exception.Message).Contains("called 1 time(s)");
    }

    // ───────────────────────── Property setters ─────────────────────────

    [Test]
    public async Task Property_Setter_WasCalled_Passes()
    {
        var mock = IPropertyHolder.Mock();
        IPropertyHolder svc = mock.Object;
        svc.Count = 10;

        await Assert.That(mock.Count.Setter).WasCalled(Times.Once);
        await Assert.That(mock.Count.Set(10)).WasCalled(Times.Once);
    }

    [Test]
    public async Task Property_Setter_WasCalled_Fails_When_Value_Differs()
    {
        var mock = IPropertyHolder.Mock();
        IPropertyHolder svc = mock.Object;
        svc.Count = 10;

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(mock.Count.Set(99)).WasCalled(Times.Once));

        await Assert.That(exception.Message).Contains("called 0 time(s)");
    }

    [Test]
    public async Task Property_Setter_WasNeverCalled_Passes_When_Only_Read()
    {
        var mock = IPropertyHolder.Mock();
        mock.Count.Returns(5);
        IPropertyHolder svc = mock.Object;
        _ = svc.Count;

        await Assert.That(mock.Count.Setter).WasNeverCalled();
    }

    // ───────────────────────── Null verification target ─────────────────────────

    [Test]
    public async Task WasCalled_Fails_For_Null_Verification()
    {
        ICallVerification verification = null!;

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(verification).WasCalled());

        await Assert.That(exception.Message).Contains("null");
    }

    [Test]
    public async Task WasNeverCalled_Fails_For_Null_Verification()
    {
        ICallVerification verification = null!;

        var exception = await Assert.ThrowsAsync<AssertionException>(async () =>
            await Assert.That(verification).WasNeverCalled());

        await Assert.That(exception.Message).Contains("null");
    }
}
