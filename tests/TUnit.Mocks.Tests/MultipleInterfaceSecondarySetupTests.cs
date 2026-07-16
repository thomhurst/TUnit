using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

/// <summary>
/// Types for testing setup/verify on SECONDARY interfaces of multi-type mocks (#4981).
/// </summary>
public interface IMultiHasInstance<T>
{
    T Instance { get; }
}

public interface IMultiConflictA
{
    string Tag { get; }
}

public interface IMultiConflictB
{
    int Tag { get; }
}

public interface IMultiDupPing
{
    void Ping();
}

public interface IMultiDupPing2
{
    void Ping();
}

public interface IMultiExtra
{
    string Instance { get; }
}

/// <summary>Models the DbContext shape from #4981: the interface member is implemented explicitly.</summary>
public class MultiServiceBase : IMultiExtra
{
    public virtual string GetName() => "real";
    string IMultiExtra.Instance => "real-instance";
}

/// <summary>The interface member is blocked by a non-virtual public implementation.</summary>
public class MultiServiceBlocked : IMultiExtra
{
    public string Instance => "blocked-real";
}

public class MultiServiceWithCtor : IMultiExtra
{
    public MultiServiceWithCtor(string seed)
    {
        Seed = seed;
    }

    public string Seed { get; }

    string IMultiExtra.Instance => Seed;
}

/// <summary>
/// Setup and verification on the secondary interfaces of Mock.Of&lt;T1, T2, ...&gt;() (#4981).
/// Secondary members surface as extensions on Mock&lt;T1&gt;, sharing the single engine.
/// </summary>
public class MultipleInterfaceSecondarySetupTests
{
    [Test]
    public async Task Can_Setup_Secondary_Interface_Method()
    {
        var mock = Mock.Of<IMultiLogger, IMultiSerializable>();
        mock.Serialize().Returns("json");

        var result = ((IMultiSerializable)mock.Object).Serialize();

        await Assert.That(result).IsEqualTo("json");
    }

    [Test]
    public async Task Can_Setup_Secondary_Interface_Property()
    {
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>();
        mock.IsDisposed.Returns(true);

        var disposed = ((IMultiDisposable)mock.Object).IsDisposed;

        await Assert.That(disposed).IsTrue();
    }

    [Test]
    public async Task Can_Verify_Secondary_Interface_Calls()
    {
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>();

        ((IMultiDisposable)mock.Object).Dispose();

        mock.Dispose().WasCalled(Times.Once);
        await Assert.That(Mock.Invocations(mock)).Count().IsEqualTo(1);
    }

    [Test]
    public async Task Can_Setup_Third_And_Fourth_Interface()
    {
        var mock = Mock.Of<IMultiLogger, IMultiDisposable, IMultiSerializable, IMultiCloneable>();
        mock.Serialize().Returns("third");
        mock.CanClone.Returns(true);

        await Assert.That(((IMultiSerializable)mock.Object).Serialize()).IsEqualTo("third");
        await Assert.That(((IMultiCloneable)mock.Object).CanClone).IsTrue();
    }

    [Test]
    public async Task Same_Pair_Works_Across_Different_Combos()
    {
        // IMultiSerializable has a different union member layout in the 2-combo vs the 4-combo;
        // the shared extension surface must resolve the right IDs for each.
        var twoCombo = Mock.Of<IMultiLogger, IMultiSerializable>();
        var fourCombo = Mock.Of<IMultiLogger, IMultiDisposable, IMultiSerializable, IMultiCloneable>();
        twoCombo.Serialize().Returns("two");
        fourCombo.Serialize().Returns("four");

        await Assert.That(((IMultiSerializable)twoCombo.Object).Serialize()).IsEqualTo("two");
        await Assert.That(((IMultiSerializable)fourCombo.Object).Serialize()).IsEqualTo("four");
    }

    [Test]
    public async Task VerifyInOrder_Interleaves_Primary_And_Secondary_Calls()
    {
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>();
        mock.Log(Any());

        mock.Object.Log("first");
        ((IMultiDisposable)mock.Object).Dispose();
        mock.Object.Log("second");

        Mock.VerifyInOrder(() =>
        {
            mock.Log(Is("first")).WasCalled();
            mock.Dispose().WasCalled();
            mock.Log(Is("second")).WasCalled();
        });
        await Assert.That(Mock.Invocations(mock)).Count().IsEqualTo(3);
    }

    [Test]
    public async Task Strict_Mode_Throws_For_Unconfigured_Secondary_Member()
    {
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>(MockBehavior.Strict);

        Assert.Throws<MockStrictBehaviorException>(() =>
        {
            ((IMultiDisposable)mock.Object).Dispose();
        });
        await Assert.That(Mock.Behavior(mock)).IsEqualTo(MockBehavior.Strict);
    }

    [Test]
    public async Task Reset_Clears_Secondary_Setups()
    {
        var mock = Mock.Of<IMultiLogger, IMultiDisposable>();
        mock.IsDisposed.Returns(true);

        Mock.Reset(mock);

        await Assert.That(((IMultiDisposable)mock.Object).IsDisposed).IsFalse();
    }

    [Test]
    public async Task Secondary_Extension_On_Plain_Mock_Throws()
    {
        // IsDisposed is a Mock<IMultiLogger> extension, so it compiles here — but this mock was
        // not created with IMultiDisposable, so configuring it must fail loudly, not no-op.
        var plain = Mock.Of<IMultiLogger>();

        var ex = Assert.Throws<InvalidOperationException>(() => _ = plain.IsDisposed);
        await Assert.That(ex.Message).Contains("IMultiDisposable");
    }

    [Test]
    public async Task Secondary_Extension_On_Wrong_Combo_Throws()
    {
        var mock = Mock.Of<IMultiLogger, IMultiSerializable>();

        var ex = Assert.Throws<InvalidOperationException>(() => _ = mock.IsDisposed);
        await Assert.That(ex.Message).Contains("IMultiDisposable");
    }

    [Test]
    public async Task Member_Shared_With_Primary_Configures_Through_Primary_Extension()
    {
        // IMultiDupPing2.Ping is signature-identical to the primary's Ping, so it deduplicates
        // onto the primary member: one extension, one engine slot, intercepting both casts.
        var mock = Mock.Of<IMultiDupPing, IMultiDupPing2>();
        mock.Ping();

        mock.Object.Ping();
        ((IMultiDupPing2)mock.Object).Ping();

        mock.Ping().WasCalled(Times.Exactly(2));
        await Assert.That(Mock.Invocations(mock)).Count().IsEqualTo(2);
    }

    [Test]
    public async Task Conflicting_Secondary_Property_Is_Prefixed_With_Interface_Name()
    {
        // IMultiConflictA.Tag (string) and IMultiConflictB.Tag (int) collide by name, so the
        // secondary surface exposes the prefixed IMultiConflictB_Tag.
        var mock = Mock.Of<IMultiConflictA, IMultiConflictB>();
        mock.Tag.Returns("primary");
        mock.IMultiConflictB_Tag.Returns(42);

        await Assert.That(mock.Object.Tag).IsEqualTo("primary");
        await Assert.That(((IMultiConflictB)mock.Object).Tag).IsEqualTo(42);
    }

    [Test]
    public async Task Class_Primary_With_Explicitly_Implemented_Interface_Member()
    {
        // The #4981 DbContext shape: class primary, interface member implemented explicitly.
        var mock = Mock.Of<MultiServiceBase, IMultiExtra>();
        mock.Instance.Returns("mocked-instance");

        var value = ((IMultiExtra)mock.Object).Instance;

        await Assert.That(value).IsEqualTo("mocked-instance");
    }

    [Test]
    public async Task Class_Primary_Unconfigured_Secondary_Member_Returns_Default()
    {
        // The mock re-implements the interface to intercept, so the base's explicit
        // implementation ("real-instance") is unreachable — unconfigured returns the smart default.
        var mock = Mock.Of<MultiServiceBase, IMultiExtra>();

        await Assert.That(((IMultiExtra)mock.Object).Instance).IsEmpty();
    }

    [Test]
    public async Task Class_Primary_Virtual_Member_Still_Behaves_As_Partial_Mock()
    {
        var mock = Mock.Of<MultiServiceBase, IMultiExtra>();

        // Unconfigured virtual member falls back to the base implementation...
        await Assert.That(mock.Object.GetName()).IsEqualTo("real");

        // ...and can still be configured.
        mock.GetName().Returns("configured");
        await Assert.That(mock.Object.GetName()).IsEqualTo("configured");
    }

    [Test]
    public async Task Class_Primary_With_NonVirtual_Blocking_Member()
    {
        var mock = Mock.Of<MultiServiceBlocked, IMultiExtra>();
        mock.Instance.Returns("mocked");

        // Interface dispatch hits the mock's explicit re-implementation...
        await Assert.That(((IMultiExtra)mock.Object).Instance).IsEqualTo("mocked");
        // ...while direct (non-virtual) access still reaches the real member.
        await Assert.That(mock.Object.Instance).IsEqualTo("blocked-real");
    }

    [Test]
    public async Task Class_Primary_With_Constructor_Args()
    {
        var mock = Mock.Of<MultiServiceWithCtor, IMultiExtra>("seeded");
        mock.Instance.Returns("from-mock");

        await Assert.That(mock.Object.Seed).IsEqualTo("seeded");
        await Assert.That(((IMultiExtra)mock.Object).Instance).IsEqualTo("from-mock");
    }

    [Test]
    public async Task Generic_Secondary_Interface()
    {
        var mock = Mock.Of<IMultiLogger, IMultiHasInstance<string>>();
        mock.Instance.Returns("generic-instance");

        var value = ((IMultiHasInstance<string>)mock.Object).Instance;

        await Assert.That(value).IsEqualTo("generic-instance");
    }
}
