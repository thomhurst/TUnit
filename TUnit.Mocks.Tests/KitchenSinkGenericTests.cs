using System.Collections;
using TUnit.Mocks;
using TUnit.Mocks.Arguments;
using TUnit.Mocks.Verification;

namespace TUnit.Mocks.Tests;

public sealed class GenericPayload
{
    public string Name { get; init; } = "";

    public override string ToString() => Name;
}

public interface IGenericProjection<TValue>
{
    TValue Current { get; }
    TValue Echo(TValue value);
}

public interface IGenericKitchenSink<TItem, TValue> : IEnumerable<TItem>
    where TItem : class
{
    TValue Convert(TItem item);
    Task<TValue> ConvertAsync(TItem item);
    ValueTask<TItem?> FindAsync(string key);
    string Format(TItem item);
    string Format(TValue value);
    string Describe(TItem item, string prefix = "item");
    bool TryGetValue(string key, out TValue value);
    void Replace(ref TValue value);
    TValue Sum(in TValue left, in TValue right);
    TState RoundTrip<TState>(TState state) where TState : struct;
    TItem? Selected { get; set; }
    int ItemCount { get; set; }
    IGenericProjection<TValue> Projection { get; }
    event Action<TItem, TValue> Changed;
}

public interface ICovariantProjection<out TValue>
{
    TValue Current { get; }
}

public interface IContravariantSink<in TValue>
{
    void Record(TValue value);
}

public interface IVariantKitchenSink<in TInput, out TOutput> :
    ICovariantProjection<TOutput>,
    IContravariantSink<TInput>
{
    TOutput Convert(TInput input);
}

public interface IVariantContainer<TInput, TOutput>
{
    IVariantKitchenSink<TInput, TOutput> Pipe { get; }
}

public abstract class GenericAbstractKitchenSink<TItem> where TItem : class
{
    private readonly TItem _fallback;

    protected GenericAbstractKitchenSink(TItem fallback)
    {
        _fallback = fallback;
    }

    public abstract TItem Create(string key);
    public abstract Task<TItem> CreateAsync(string key);
    public abstract TItem Current { get; set; }

    public virtual TItem Echo(TItem item) => item;
    public virtual Task<TItem> EchoAsync(TItem item) => Task.FromResult(item);
    public virtual string Render(TItem item) => item.ToString() ?? "";
    public virtual string Render(TItem item, string prefix) => $"{prefix}:{item}";

    public virtual bool TryResolve(string key, out TItem? item)
    {
        item = _fallback;
        return true;
    }

    public virtual void Reset(ref TItem? item)
    {
        item = _fallback;
    }

    public virtual int Measure(in TItem item) => item.ToString()?.Length ?? 0;
    public virtual string Describe() => _fallback.ToString() ?? "";
    public virtual TState EchoState<TState>(TState state) where TState : struct => state;

    public virtual event Action<TItem>? Updated;

    public void RaiseUpdated(TItem item) => Updated?.Invoke(item);
}

public class KitchenSinkGenericTests
{
    [Test]
    public async Task Generic_Interface_Closed_Methods_Are_Configurable()
    {
        var mock = IGenericKitchenSink<GenericPayload, int>.Mock();
        var payload = new GenericPayload { Name = "alpha" };
        mock.Convert(payload).Returns(10);
        mock.ConvertAsync(payload).Returns(11);
        mock.FindAsync("alpha").Returns(payload);
        mock.Format(payload).Returns("item-alpha");
        mock.Format(10).Returns("value-10");
        mock.Describe(payload, "item").Returns("item:alpha");

        await Assert.That(mock.Object.Convert(payload)).IsEqualTo(10);
        await Assert.That(await mock.Object.ConvertAsync(payload)).IsEqualTo(11);
        await Assert.That(await mock.Object.FindAsync("alpha")).IsSameReferenceAs(payload);
        await Assert.That(mock.Object.Format(payload)).IsEqualTo("item-alpha");
        await Assert.That(mock.Object.Format(10)).IsEqualTo("value-10");
        await Assert.That(mock.Object.Describe(payload)).IsEqualTo("item:alpha");
    }

    [Test]
    public async Task Generic_Interface_In_Out_And_Ref_Work()
    {
        var mock = IGenericKitchenSink<GenericPayload, int>.Mock();
        mock.TryGetValue("answer").Returns(true).SetsOutValue(42);
        mock.Sum(3, 4).Returns(7);
        mock.Replace(Any()).SetsRefValue(99);

        var found = mock.Object.TryGetValue("answer", out var value);
        int current = 5;
        var sum = mock.Object.Sum(3, 4);
        mock.Object.Replace(ref current);

        await Assert.That(found).IsTrue();
        await Assert.That(value).IsEqualTo(42);
        await Assert.That(sum).IsEqualTo(7);
        await Assert.That(current).IsEqualTo(99);
    }

    [Test]
    public async Task Generic_Interface_Method_On_Generic_Type_Works()
    {
        var mock = IGenericKitchenSink<GenericPayload, int>.Mock();
        mock.RoundTrip<Guid>(Any()).Returns(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        var result = mock.Object.RoundTrip(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        await Assert.That(result).IsEqualTo(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
    }

    [Test]
    public async Task Generic_Interface_Properties_Events_And_Enumeration_Work()
    {
        var mock = IGenericKitchenSink<GenericPayload, int>.Mock();
        var first = new GenericPayload { Name = "first" };
        var second = new GenericPayload { Name = "second" };
        mock.Selected.Returns(first);
        mock.ItemCount.Returns(2);
        mock.GetEnumerator().Returns(new List<GenericPayload> { first, second }.GetEnumerator());

        GenericPayload? raisedItem = null;
        int raisedValue = 0;
        mock.Object.Changed += (item, value) =>
        {
            raisedItem = item;
            raisedValue = value;
        };

        var seen = new List<GenericPayload>();
        foreach (var item in mock.Object)
        {
            seen.Add(item);
        }

        mock.RaiseChanged(second, 12);
        mock.Object.ItemCount = 3;

        await Assert.That(mock.Object.Selected).IsSameReferenceAs(first);
        await Assert.That(mock.Object.ItemCount).IsEqualTo(2);
        await Assert.That(seen.Select(x => x.Name)).IsEquivalentTo(["first", "second"]);
        await Assert.That(raisedItem).IsSameReferenceAs(second);
        await Assert.That(raisedValue).IsEqualTo(12);
        mock.ItemCount.Set(3).WasCalled(Times.Once);
    }

    [Test]
    public async Task Generic_Interface_Explicit_Non_Generic_Enumeration_Works()
    {
        var mock = IGenericKitchenSink<GenericPayload, int>.Mock();
        mock.GetEnumerator().Returns(new List<GenericPayload>
        {
            new() { Name = "one" }
        }.GetEnumerator());

        IEnumerable enumerable = mock.Object;
        int count = 0;
        foreach (var _ in enumerable)
        {
            count++;
        }

        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async Task Generic_Interface_Transitive_Auto_Mock_Is_Functional()
    {
        var mock = IGenericKitchenSink<GenericPayload, int>.Mock();

        var projection = mock.Object.Projection;
        var projectionMock = Mock.Get(projection);
        projectionMock.Current.Returns(123);
        projectionMock.Echo(Any()).Returns(456);

        await Assert.That(projection.Current).IsEqualTo(123);
        await Assert.That(projection.Echo(10)).IsEqualTo(456);
    }

    [Test]
    public async Task Variant_Interface_Covariant_And_Contravariant_Members_Work()
    {
        var mock = IVariantKitchenSink<string, GenericPayload>.Mock();
        var payload = new GenericPayload { Name = "variant" };
        mock.Convert("alpha").Returns(payload);
        mock.Current.Returns(payload);

        mock.Object.Record("alpha");

        await Assert.That(mock.Object.Convert("alpha")).IsSameReferenceAs(payload);
        await Assert.That(mock.Object.Current).IsSameReferenceAs(payload);
        mock.Record("alpha").WasCalled(Times.Once);
    }

    [Test]
    public async Task Variant_Interface_Transitive_Auto_Mock_Is_Functional()
    {
        var mock = IVariantContainer<string, GenericPayload>.Mock();
        var payload = new GenericPayload { Name = "payload" };

        var pipe = mock.Object.Pipe;
        var pipeMock = Mock.Get(pipe);
        pipeMock.Convert("alpha").Returns(payload);
        pipeMock.Current.Returns(payload);

        pipe.Record("alpha");

        await Assert.That(pipe.Convert("alpha")).IsSameReferenceAs(payload);
        await Assert.That(pipe.Current).IsSameReferenceAs(payload);
        pipeMock.Record("alpha").WasCalled(Times.Once);
    }

    [Test]
    public async Task Generic_Interface_Verification_Works()
    {
        var mock = IGenericKitchenSink<GenericPayload, int>.Mock();
        var payload = new GenericPayload { Name = "verify" };
        mock.Convert(Any()).Returns(1);
        mock.RoundTrip<int>(Any()).Returns(2);

        mock.Object.Convert(payload);
        mock.Object.Convert(payload);
        mock.Object.RoundTrip(5);

        mock.Convert(Any()).WasCalled(Times.Exactly(2));
        mock.RoundTrip<int>(5).WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Generic_Abstract_Abstract_Members_Are_Configurable()
    {
        var seed = new GenericPayload { Name = "seed" };
        var created = new GenericPayload { Name = "created" };
        var mock = GenericAbstractKitchenSink<GenericPayload>.Mock(seed);
        mock.Create("key").Returns(created);
        mock.CreateAsync("key").Returns(created);
        mock.Current.Returns(created);

        await Assert.That(mock.Object.Create("key")).IsSameReferenceAs(created);
        await Assert.That(await mock.Object.CreateAsync("key")).IsSameReferenceAs(created);
        await Assert.That(mock.Object.Current).IsSameReferenceAs(created);
    }

    [Test]
    public async Task Generic_Abstract_Virtuals_Use_Base_When_Unconfigured()
    {
        var seed = new GenericPayload { Name = "seed-value" };
        var echo = new GenericPayload { Name = "echo" };
        var mock = GenericAbstractKitchenSink<GenericPayload>.Mock(seed);
        mock.Create(Any()).Returns(seed);
        mock.CreateAsync(Any()).Returns(seed);
        mock.Current.Returns(seed);

        await Assert.That(mock.Object.Echo(echo)).IsSameReferenceAs(echo);
        await Assert.That(await mock.Object.EchoAsync(echo)).IsSameReferenceAs(echo);
        await Assert.That(mock.Object.Measure(echo)).IsEqualTo(4);
        await Assert.That(mock.Object.Describe()).IsEqualTo("seed-value");
        await Assert.That(mock.Object.Render(echo)).IsEqualTo("echo");
        await Assert.That(mock.Object.Render(echo, "prefix")).IsEqualTo("prefix:echo");
    }

    [Test]
    public async Task Generic_Abstract_In_Out_And_Ref_Work()
    {
        var seed = new GenericPayload { Name = "seed" };
        var replacement = new GenericPayload { Name = "replacement" };
        var configured = new GenericPayload { Name = "configured" };
        var mock = GenericAbstractKitchenSink<GenericPayload>.Mock(seed);
        mock.Create(Any()).Returns(seed);
        mock.CreateAsync(Any()).Returns(seed);
        mock.Current.Returns(seed);
        mock.TryResolve("item").Returns(true).SetsOutItem(configured);
        mock.Reset(Any()).SetsRefItem(replacement);

        var found = mock.Object.TryResolve("item", out var resolved);
        GenericPayload? current = seed;
        mock.Object.Reset(ref current);

        await Assert.That(found).IsTrue();
        await Assert.That(resolved).IsSameReferenceAs(configured);
        await Assert.That(current).IsSameReferenceAs(replacement);
    }

    [Test]
    public async Task Generic_Abstract_Generic_Method_And_Event_Work()
    {
        var seed = new GenericPayload { Name = "seed" };
        var mock = GenericAbstractKitchenSink<GenericPayload>.Mock(seed);
        mock.Create(Any()).Returns(seed);
        mock.CreateAsync(Any()).Returns(seed);
        mock.Current.Returns(seed);
        mock.EchoState<int>(Any()).Returns(12);

        GenericPayload? updated = null;
        mock.Object.Updated += item => updated = item;

        var value = mock.Object.EchoState(8);
        mock.RaiseUpdated(seed);

        await Assert.That(value).IsEqualTo(12);
        await Assert.That(updated).IsSameReferenceAs(seed);
    }

    [Test]
    public async Task Generic_Abstract_Verification_Works()
    {
        var seed = new GenericPayload { Name = "seed" };
        var mock = GenericAbstractKitchenSink<GenericPayload>.Mock(seed);
        mock.Create(Any()).Returns(seed);
        mock.Current.Returns(seed);

        mock.Object.Create("a");
        mock.Object.Create("b");
        _ = mock.Object.Current;

        mock.Create(Any()).WasCalled(Times.Exactly(2));
        mock.Current.WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }
}
