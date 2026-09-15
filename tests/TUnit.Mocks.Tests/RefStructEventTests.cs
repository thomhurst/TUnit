#if NET9_0_OR_GREATER
[assembly: TUnit.Mocks.GenerateMock(typeof(TUnit.Mocks.Tests.RefStructEventTests.IAntiConstrained<>))]
#endif

namespace TUnit.Mocks.Tests;

public class RefStructEventTests
{
    public readonly ref struct Payload(int value)
    {
        public int Value { get; } = value;
    }

    public delegate void PayloadHandler(Payload payload);
    public delegate void BufferHandler(string name, ReadOnlySpan<int> values, Payload payload);
    public delegate void MutableBufferHandler(Span<int> values);
    public delegate void GenericBufferHandler<T>(ReadOnlySpan<T> values) where T : unmanaged;
    public delegate void RefPayloadHandler(ref Payload payload);
    public delegate void InPayloadHandler(in Payload payload);
    public delegate void OutPayloadHandler(out Payload payload);
    public delegate void MixedHandler(ref Payload payload, in ReadOnlySpan<int> values, out Payload result);
    public delegate void RefIntHandler(ref int value);

    public interface IByReferenceEvents
    {
        event RefPayloadHandler? RefChanged;
        event InPayloadHandler? InChanged;
        event OutPayloadHandler? OutChanged;
        event MixedHandler? MixedChanged;
        event RefIntHandler? IntChanged;
    }

#if NET9_0_OR_GREATER
    public delegate void AntiConstrainedHandler<T>(T value) where T : allows ref struct;
    public interface IAntiConstrained<T> where T : allows ref struct
    {
        event AntiConstrainedHandler<T>? AntiConstrainedChanged;
    }

    private static void RaiseAntiConstrained<T>(Mock<IAntiConstrained<T>> mock, T value) where T : allows ref struct
        => mock.RaiseAntiConstrainedChanged(value);

    [Test]
    public async Task AntiConstrained_Generic_Event_Accepts_RefStruct_And_Boxable_Arguments()
    {
        var stackOnly = Mock.Of<IAntiConstrained<Payload>>();
        var boxable = Mock.Of<IAntiConstrained<int>>();
        var received = 0;
        stackOnly.Object.AntiConstrainedChanged += value => received += value.Value;
        boxable.Object.AntiConstrainedChanged += value => received += value;

        RaiseAntiConstrained(stackOnly, new Payload(2));
        RaiseAntiConstrained(boxable, 3);

        await Assert.That(received).IsEqualTo(5);
    }
#endif

    public interface IEvents
    {
        event PayloadHandler? Changed;
        event BufferHandler? Buffered;
        event MutableBufferHandler? Mutable;
        event EventHandler<string>? Ordinary;
        void Execute();
        int Query();
    }

    public interface ISecondary
    {
        event PayloadHandler? Secondary;
    }

    public interface IExtra { }
    public interface IInherited : IEvents { }
    public interface IGeneric<T> where T : unmanaged
    {
        event GenericBufferHandler<T>? Generic;
    }

    public abstract class Service
    {
        public abstract event PayloadHandler? ServiceChanged;
        public abstract event OutPayloadHandler? ServiceProduced;
    }

    public class WrappedService
    {
        public virtual event PayloadHandler? WrappedChanged;
        public virtual event OutPayloadHandler? WrappedProduced;
        public virtual void Execute() => WrappedChanged?.Invoke(new Payload(99));
        public virtual void Produce(out Payload payload)
        {
            payload = default;
            WrappedProduced?.Invoke(out payload);
        }
    }

#if NET10_0_OR_GREATER
    public interface IEventHandlerEvents
    {
        event EventHandler<Payload>? PayloadChanged;
        event EventHandler<ReadOnlySpan<int>>? SpanChanged;
    }

    [Test]
    [Arguments(MockBehavior.Strict)]
    [Arguments(MockBehavior.Loose)]
    public async Task EventHandler_Preserves_Payload_And_Sender(MockBehavior behavior)
    {
        var mock = Mock.Of<IEventHandlerEvents>(behavior);
        object? sender = null;
        var received = 0;
        mock.Object.PayloadChanged += (s, e) => { sender = s; received = e.Value; };

        mock.RaisePayloadChanged(new Payload(42));

        await Assert.That(received).IsEqualTo(42);
        await Assert.That(sender).IsSameReferenceAs(MockRegistry.GetEngine(mock).Raisable);
    }

    [Test]
    public async Task EventHandler_ReadOnlySpan_Reaches_Subscriber()
    {
        var mock = Mock.Of<IEventHandlerEvents>();
        var sum = 0;
        mock.Object.SpanChanged += (_, values) => { foreach (var value in values) sum += value; };

        mock.RaiseSpanChanged(stackalloc int[] { 2, 3, 5 });

        await Assert.That(sum).IsEqualTo(10);
    }
#endif

    [Test]
    [Arguments(MockBehavior.Strict)]
    [Arguments(MockBehavior.Loose)]
    public async Task Custom_Delegate_Preserves_Payload(MockBehavior behavior)
    {
        var mock = Mock.Of<IEvents>(behavior);
        var received = 0;
        mock.Object.Changed += e => received = e.Value;

        mock.RaiseChanged(new Payload(42));

        await Assert.That(received).IsEqualTo(42);
    }

    [Test]
    public async Task Multi_Parameter_Delegate_Preserves_All_Arguments()
    {
        var mock = Mock.Of<IEvents>();
        string? name = null;
        var received = 0;
        mock.Object.Buffered += (n, values, e) => { name = n; received = values[0] + values[1] + e.Value; };

        mock.RaiseBuffered("buffer", stackalloc int[] { 2, 3 }, new Payload(5));

        await Assert.That(name).IsEqualTo("buffer");
        await Assert.That(received).IsEqualTo(10);
    }

    [Test]
    public async Task Mutable_Span_Changes_Reach_Caller()
    {
        var mock = Mock.Of<IEvents>();
        mock.Object.Mutable += values => values[0] = 42;
        Span<int> values = stackalloc int[] { 1 };

        mock.RaiseMutable(values);
        var received = values[0];

        await Assert.That(received).IsEqualTo(42);
    }

    [Test]
    public async Task Subscribers_Run_In_Order_And_Unsubscribe_Works()
    {
        var mock = Mock.Of<IEvents>();
        var received = new List<int>();
        PayloadHandler first = e => received.Add(e.Value);
        PayloadHandler second = e => received.Add(e.Value * 10);
        mock.Object.Changed += first;
        mock.Object.Changed += second;

        mock.RaiseChanged(new Payload(1));
        mock.Object.Changed -= first;
        mock.RaiseChanged(new Payload(2));
        mock.Object.Changed -= second;
        mock.RaiseChanged(new Payload(3));

        await Assert.That(string.Join(",", received)).IsEqualTo("1,10,20");
        await Assert.That(mock.Events.Changed.SubscriberCount).IsEqualTo(0);
        await Assert.That(mock.Events.Changed.WasSubscribed).IsTrue();
    }

    [Test]
    public void Raise_Without_Subscribers_Does_Not_Throw()
    {
        var mock = Mock.Of<IEvents>(MockBehavior.Strict);
        mock.RaiseChanged(new Payload(42));
        mock.RaiseBuffered("empty", ReadOnlySpan<int>.Empty, new Payload(0));
    }

    [Test]
    public async Task Subscriber_Exception_Propagates()
    {
        var mock = Mock.Of<IEvents>();
        mock.Object.Changed += _ => throw new InvalidOperationException("subscriber failed");

        await Assert.That(() => mock.RaiseChanged(new Payload(42)))
            .Throws<InvalidOperationException>().WithMessage("subscriber failed");
    }

    [Test]
    public async Task Callback_Creates_Fresh_Arguments_And_Ordinary_Auto_Raise_Still_Works()
    {
        var mock = Mock.Of<IEvents>(MockBehavior.Strict);
        var received = new List<int>();
        string? ordinary = null;
        var next = 0;
        mock.Object.Changed += e => received.Add(e.Value);
        mock.Object.Ordinary += (_, e) => ordinary = e;
        mock.Execute().Callback(() => mock.RaiseChanged(new Payload(++next))).RaisesOrdinary("void");
        mock.Query().Returns(42).Callback(() => mock.RaiseChanged(new Payload(++next))).RaisesOrdinary("query");

        mock.Object.Execute();
        var result = mock.Object.Query();

        await Assert.That(string.Join(",", received)).IsEqualTo("1,2");
        await Assert.That(result).IsEqualTo(42);
        await Assert.That(ordinary).IsEqualTo("query");
    }

    [Test]
    public async Task Boxed_Dispatch_Rejects_RefStruct_Events_Clearly()
    {
        var mock = Mock.Of<IEvents>();
        var raisable = MockRegistry.GetEngine(mock).Raisable!;

        await Assert.That(() => raisable.RaiseEvent("Changed", null))
            .Throws<NotSupportedException>()
            .WithMessage("Event 'Changed' has ref struct parameters and cannot be raised with boxed arguments.");
        await Assert.That(() => raisable.RaiseEvent("Missing", null)).Throws<InvalidOperationException>();
    }

    [Test]
    public async Task Inherited_And_Generic_Events_Can_Be_Raised()
    {
        var inherited = Mock.Of<IInherited>();
        var generic = Mock.Of<IGeneric<int>>();
        var received = 0;
        inherited.Object.Changed += e => received += e.Value;
        generic.Object.Generic += values => received += values[0];

        inherited.RaiseChanged(new Payload(2));
        generic.RaiseGeneric(stackalloc int[] { 3 });

        await Assert.That(received).IsEqualTo(5);
    }

    [Test]
    public async Task Multi_Type_Primary_And_Secondary_Events_Use_Shared_Extensions()
    {
        var pair = Mock.Of<IEvents, ISecondary>();
        var triple = Mock.Of<IEvents, IExtra, ISecondary>();
        var received = 0;
        pair.Object.Changed += e => received += e.Value;
        ((ISecondary)pair.Object).Secondary += e => received += e.Value;
        triple.Object.Changed += e => received += e.Value;
        ((ISecondary)triple.Object).Secondary += e => received += e.Value;

        pair.RaiseChanged(new Payload(1));
        pair.RaiseSecondary(new Payload(2));
        triple.RaiseChanged(new Payload(4));
        triple.RaiseSecondary(new Payload(8));

        await Assert.That(received).IsEqualTo(15);
    }

    [Test]
    public async Task Partial_Mock_And_Additional_Interface_Events_Can_Be_Raised()
    {
        var mock = Mock.Of<Service, ISecondary>();
        var received = 0;
        mock.Object.ServiceChanged += e => received += e.Value;
        ((ISecondary)mock.Object).Secondary += e => received += e.Value;

        mock.RaiseServiceChanged(new Payload(2));
        mock.RaiseSecondary(new Payload(3));

        await Assert.That(received).IsEqualTo(5);
    }

    [Test]
    public async Task Ref_Changes_Reach_Later_Subscribers_And_Caller()
    {
        var mock = Mock.Of<IByReferenceEvents>();
        mock.Object.RefChanged += (ref Payload value) => value = new Payload(value.Value + 1);
        mock.Object.RefChanged += (ref Payload value) => value = new Payload(value.Value * 10);
        var payload = new Payload(1);

        mock.RaiseRefChanged(ref payload);
        var received = payload.Value;

        await Assert.That(received).IsEqualTo(20);
    }

    [Test]
    public async Task In_Parameter_Reaches_Subscriber()
    {
        var mock = Mock.Of<IByReferenceEvents>();
        var received = 0;
        mock.Object.InChanged += (in Payload value) => received = value.Value;
        var payload = new Payload(42);

        mock.RaiseInChanged(in payload);

        await Assert.That(received).IsEqualTo(42);
    }

    [Test]
    public async Task Out_Parameter_Uses_Last_Subscriber_And_Default_When_Unsubscribed()
    {
        var mock = Mock.Of<IByReferenceEvents>();
        OutPayloadHandler first = (out Payload value) => value = new Payload(1);
        OutPayloadHandler last = (out Payload value) => value = new Payload(42);
        mock.Object.OutChanged += first;
        mock.Object.OutChanged += last;

        mock.RaiseOutChanged(out var payload);
        var received = payload.Value;
        mock.Object.OutChanged -= first;
        mock.Object.OutChanged -= last;
        mock.RaiseOutChanged(out payload);
        var empty = payload.Value;

        await Assert.That(received).IsEqualTo(42);
        await Assert.That(empty).IsEqualTo(0);
    }

    [Test]
    public async Task Mixed_Reference_Directions_Preserve_Arguments()
    {
        var mock = Mock.Of<IByReferenceEvents>();
        mock.Object.MixedChanged += (ref Payload value, in ReadOnlySpan<int> values, out Payload result) =>
        {
            value = new Payload(value.Value + values[0]);
            result = new Payload(value.Value * 2);
        };
        var payload = new Payload(2);
        ReadOnlySpan<int> values = new int[] { 3 };

        mock.RaiseMixedChanged(ref payload, in values, out var result);
        var updated = payload.Value;
        var produced = result.Value;

        await Assert.That(updated).IsEqualTo(5);
        await Assert.That(produced).IsEqualTo(10);
    }

    [Test]
    public async Task Boxable_ByReference_Event_Also_Preserves_Caller_Reference()
    {
        var mock = Mock.Of<IByReferenceEvents>();
        mock.Object.IntChanged += (ref int value) => value++;
        var received = 41;

        mock.RaiseIntChanged(ref received);

        await Assert.That(received).IsEqualTo(42);
        await Assert.That(() => MockRegistry.GetEngine(mock).Raisable!.RaiseEvent("IntChanged", 41))
            .Throws<NotSupportedException>();
    }

    [Test]
    public async Task Partial_And_Wrap_Out_Events_Assign_Results()
    {
        var partial = Mock.Of<Service>();
        var wrapped = Mock.Wrap(new WrappedService());
        partial.RaiseServiceProduced(out var beforePartial);
        wrapped.RaiseWrappedProduced(out var beforeWrapped);
        var empty = beforePartial.Value + beforeWrapped.Value;
        partial.Object.ServiceProduced += (out Payload value) => value = new Payload(2);
        wrapped.Object.WrappedProduced += (out Payload value) => value = new Payload(3);

        partial.RaiseServiceProduced(out var partialResult);
        wrapped.RaiseWrappedProduced(out var wrappedResult);
        var received = partialResult.Value + wrappedResult.Value;

        await Assert.That(empty).IsEqualTo(0);
        await Assert.That(received).IsEqualTo(5);
    }

    [Test]
    public async Task Wrap_Mock_Event_Can_Be_Raised()
    {
        var mock = Mock.Wrap(new WrappedService());
        var received = 0;
        mock.Object.WrappedChanged += e => received = e.Value;

        mock.RaiseWrappedChanged(new Payload(42));

        await Assert.That(received).IsEqualTo(42);
    }
}
