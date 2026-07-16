using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Verifies that source-generated extension members never collide with framework members on
/// <see cref="Mock{T}"/>. Each interface here defines members whose names match either the
/// one deliberately-public Mock&lt;T&gt; instance property (<c>Object</c>) or the inherited
/// <c>object</c> methods (<c>Equals</c>, <c>GetHashCode</c>, <c>ToString</c>). All framework
/// operations live on <see cref="IMockControl{T}"/> as explicit interface implementations and
/// are reached via <see cref="Mock"/> static helpers, so members named <c>Reset</c>,
/// <c>VerifyAll</c>, <c>Invocations</c>, etc. on the mocked interface are reachable as
/// generated extensions without conflict.
/// </summary>
public class NameCollisionTests
{
    public interface IFrameworkNameMember
    {
        // Names that previously lived as public instance members on Mock<T>. They must
        // now be reachable as generated setup/verify extensions, not shadowed by Mock<T>.
        int Reset();
        int VerifyAll();
        int VerifyNoOtherCalls();
        int Invocations();
        int Behavior();
        int SetupAllProperties();
        int GetDiagnostics();
        int SetState(string s);
        int InState();
        int DefaultValueProvider();
    }

    [Test]
    public async Task Generated_Extensions_Win_Over_Former_Framework_Member_Names()
    {
        var mock = IFrameworkNameMember.Mock();

        mock.Reset().Returns(1);
        mock.VerifyAll().Returns(2);
        mock.VerifyNoOtherCalls().Returns(3);
        mock.Invocations().Returns(4);
        mock.Behavior().Returns(5);
        mock.SetupAllProperties().Returns(6);
        mock.GetDiagnostics().Returns(7);
        mock.SetState(Any()).Returns(8);
        mock.InState().Returns(9);
        mock.DefaultValueProvider().Returns(10);

        var svc = mock.Object;
        await Assert.That(svc.Reset()).IsEqualTo(1);
        await Assert.That(svc.VerifyAll()).IsEqualTo(2);
        await Assert.That(svc.VerifyNoOtherCalls()).IsEqualTo(3);
        await Assert.That(svc.Invocations()).IsEqualTo(4);
        await Assert.That(svc.Behavior()).IsEqualTo(5);
        await Assert.That(svc.SetupAllProperties()).IsEqualTo(6);
        await Assert.That(svc.GetDiagnostics()).IsEqualTo(7);
        await Assert.That(svc.SetState("x")).IsEqualTo(8);
        await Assert.That(svc.InState()).IsEqualTo(9);
        await Assert.That(svc.DefaultValueProvider()).IsEqualTo(10);

        mock.Reset().WasCalled(Times.Once);
        mock.SetState("x").WasCalled(Times.Once);

        // Framework operations remain reachable via the static helpers.
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(10);
        Mock.Reset(mock);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }

    public interface IObjectCollision
    {
        int Object { get; }
    }

    [Test]
    public async Task User_Object_Property_Is_Reachable_Via_Renamed_Extension()
    {
        var mock = IObjectCollision.Mock();
        // The Mock<T>.Object property still exists, so the generated extension is renamed.
        // The single-trailing-underscore form is the primary rename.
        mock.Object_.Returns(42);

        await Assert.That(mock.Object.Object).IsEqualTo(42);
    }

    public interface IObjectOverflow
    {
        int Object { get; }
        int Object_ { get; }
    }

    [Test]
    public async Task Object_And_Object_Underscore_Both_Reachable_With_Iterative_Suffix()
    {
        var mock = IObjectOverflow.Mock();
        // User's Object property — extension renamed past the user-defined Object_ to Object__.
        mock.Object__.Returns(7);
        // User's Object_ property — name doesn't clash with framework, kept as Object_.
        mock.Object_.Returns(11);

        await Assert.That(mock.Object.Object).IsEqualTo(7);
        await Assert.That(mock.Object.Object_).IsEqualTo(11);
    }

    public interface IEqualsCollision
    {
        bool Equals(string other);
        int GetHashCode(int seed);
        string ToString(string format);
    }

    [Test]
    public async Task Inherited_Object_Method_Names_Use_Of_Suffix()
    {
        var mock = IEqualsCollision.Mock();
        mock.EqualsOf(Any()).Returns(true);
        mock.GetHashCodeOf(Any()).Returns(99);
        mock.ToStringOf(Any()).Returns("formatted");

        var svc = mock.Object;
        await Assert.That(svc.Equals("x")).IsTrue();
        await Assert.That(svc.GetHashCode(5)).IsEqualTo(99);
        await Assert.That(svc.ToString("D")).IsEqualTo("formatted");
    }

    public interface IPolyfillSubject
    {
        // Plain interface — no member names that overlap with framework operations,
        // so the net9.0+ generator polyfills emit instance-style ergonomics for
        // Reset / VerifyAll / Invocations / etc.
        int GetValue();
    }

#if NET9_0_OR_GREATER
    [Test]
    public async Task Polyfill_Restores_Instance_Style_Mock_Reset_On_Net9_Plus()
    {
        var mock = IPolyfillSubject.Mock();
        mock.GetValue().Returns(7);

        await Assert.That(mock.Object.GetValue()).IsEqualTo(7);
        await Assert.That(mock.Invocations.Count).IsEqualTo(1);

        mock.Reset();

        await Assert.That(mock.Invocations.Count).IsEqualTo(0);
        await Assert.That(mock.Object.GetValue()).IsEqualTo(0);
    }

    [Test]
    public async Task Polyfill_VerifyAll_And_VerifyNoOtherCalls_Reachable()
    {
        var mock = IPolyfillSubject.Mock();
        mock.GetValue().Returns(1);
        _ = mock.Object.GetValue();

        // No throw — setup invoked, no unverified calls outside the WasCalled.
        mock.GetValue().WasCalled();
        mock.VerifyNoOtherCalls();
        mock.VerifyAll();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Polyfill_Property_Getters_And_Setter_Are_Reachable()
    {
        var mock = IPolyfillSubject.Mock();
        await Assert.That(mock.Behavior).IsEqualTo(MockBehavior.Loose);
        await Assert.That(mock.Invocations).IsNotNull();

        var provider = new NoopDefaults();
        mock.DefaultValueProvider = provider;
        await Assert.That(mock.DefaultValueProvider).IsSameReferenceAs(provider);
    }

    private sealed class NoopDefaults : IDefaultValueProvider
    {
        public bool CanProvide(Type type) => false;
        public object? GetDefaultValue(Type type) => null;
    }

    [Test]
    public async Task Polyfill_Skipped_When_User_Member_Collides()
    {
        // IFrameworkNameMember.Reset is a user-declared method returning int.
        // The polyfill must NOT emit a competing void Reset(this Mock<T>) — the user's
        // generated setup extension is the only mock.Reset(...) form. Reaching the
        // framework operation requires Mock.Reset(mock).
        var mock = IFrameworkNameMember.Mock();
        mock.Reset().Returns(99);
        await Assert.That(mock.Object.Reset()).IsEqualTo(99);

        // The static helper remains the canonical way to clear setups.
        Mock.Reset(mock);
        await Assert.That(Mock.Invocations(mock).Count).IsEqualTo(0);
    }
#endif

    public interface IEqualsOfOverflow
    {
        bool Equals(string other);
        bool EqualsOf(string other);
    }

    [Test]
    public async Task EqualsOf_Overflow_Escalates_With_Underscore()
    {
        var mock = IEqualsOfOverflow.Mock();
        // User's Equals — primary rename EqualsOf collides with user-defined EqualsOf,
        // iterative suffix escalates to EqualsOf_.
        mock.EqualsOf_(Any()).Returns(true);
        // User's literal EqualsOf — unchanged (only Equals/GetHashCode/ToString are renamed).
        mock.EqualsOf(Any()).Returns(false);

        var svc = mock.Object;
        await Assert.That(svc.Equals("x")).IsTrue();
        await Assert.That(svc.EqualsOf("x")).IsFalse();
    }
}
