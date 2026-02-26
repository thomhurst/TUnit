using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// ─── Interfaces with span-returning methods ─────────────────────────────────

public interface ISpanProducer
{
    ReadOnlySpan<byte> GetBytes();
    ReadOnlySpan<byte> GetBytes(string key);
    ReadOnlySpan<char> GetChars(int id);
    Span<byte> GetMutableBuffer();
}

// ─── Tests ──────────────────────────────────────────────────────────────────

/// <summary>
/// Tests for methods returning ReadOnlySpan/Span types, verifying .Returns() support,
/// callbacks, throws, verification, and default behavior.
/// </summary>
public class SpanReturnTests
{
    [Test]
    public async Task Returns_ReadOnlySpan_Byte_With_Data()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes().Returns(new ReadOnlySpan<byte>([1, 2, 3]));

        var result = mock.Object.GetBytes();
        var len = result.Length;
        var b0 = result[0];
        var b1 = result[1];
        var b2 = result[2];

        await Assert.That(len).IsEqualTo(3);
        await Assert.That(b0).IsEqualTo((byte)1);
        await Assert.That(b1).IsEqualTo((byte)2);
        await Assert.That(b2).IsEqualTo((byte)3);
    }

    [Test]
    public async Task Returns_ReadOnlySpan_Byte_Empty()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes().Returns(ReadOnlySpan<byte>.Empty);

        var result = mock.Object.GetBytes();
        var len = result.Length;

        await Assert.That(len).IsEqualTo(0);
    }

    [Test]
    public async Task Returns_ReadOnlySpan_No_Setup_Returns_Default()
    {
        // No .Returns() call — should return empty span (default)
        var mock = Mock.Of<ISpanProducer>();

        var result = mock.Object.GetBytes();
        var len = result.Length;

        await Assert.That(len).IsEqualTo(0);
    }

    [Test]
    public async Task Returns_ReadOnlySpan_With_Arg_Matching()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes("hello").Returns(new ReadOnlySpan<byte>([0xCA, 0xFE]));
        mock.GetBytes("world").Returns(new ReadOnlySpan<byte>([0xDE, 0xAD]));

        var r1 = mock.Object.GetBytes("hello");
        var r1Len = r1.Length;
        var r1b0 = r1[0];
        var r1b1 = r1[1];

        var r2 = mock.Object.GetBytes("world");
        var r2Len = r2.Length;
        var r2b0 = r2[0];
        var r2b1 = r2[1];

        await Assert.That(r1Len).IsEqualTo(2);
        await Assert.That(r1b0).IsEqualTo((byte)0xCA);
        await Assert.That(r1b1).IsEqualTo((byte)0xFE);

        await Assert.That(r2Len).IsEqualTo(2);
        await Assert.That(r2b0).IsEqualTo((byte)0xDE);
        await Assert.That(r2b1).IsEqualTo((byte)0xAD);
    }

    [Test]
    public async Task Returns_ReadOnlySpan_With_Arg_Any()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes(Arg.Any<string>()).Returns(new ReadOnlySpan<byte>([0xFF]));

        var result = mock.Object.GetBytes("anything");
        var len = result.Length;
        var b0 = result[0];

        await Assert.That(len).IsEqualTo(1);
        await Assert.That(b0).IsEqualTo((byte)0xFF);
    }

    [Test]
    public async Task Returns_ReadOnlySpan_Char()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetChars(42).Returns(new ReadOnlySpan<char>(['a', 'b', 'c']));

        var result = mock.Object.GetChars(42);
        var len = result.Length;
        var c0 = result[0];
        var c1 = result[1];
        var c2 = result[2];

        await Assert.That(len).IsEqualTo(3);
        await Assert.That(c0).IsEqualTo('a');
        await Assert.That(c1).IsEqualTo('b');
        await Assert.That(c2).IsEqualTo('c');
    }

    [Test]
    public async Task Returns_Mutable_Span()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetMutableBuffer().Returns(new Span<byte>([10, 20, 30]));

        var result = mock.Object.GetMutableBuffer();
        var len = result.Length;
        var b0 = result[0];
        var b1 = result[1];
        var b2 = result[2];

        await Assert.That(len).IsEqualTo(3);
        await Assert.That(b0).IsEqualTo((byte)10);
        await Assert.That(b1).IsEqualTo((byte)20);
        await Assert.That(b2).IsEqualTo((byte)30);
    }

    [Test]
    public void Span_Return_Throws_Exception()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes().Throws<InvalidOperationException>();

        Assert.Throws<InvalidOperationException>(() => mock.Object.GetBytes());
    }

    [Test]
    public async Task Span_Return_Callback_Is_Invoked()
    {
        var wasCalled = false;
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes().Callback(() => wasCalled = true)
            .Returns(new ReadOnlySpan<byte>([1]));

        mock.Object.GetBytes();

        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task Span_Return_Verify_WasCalled()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes().Returns(new ReadOnlySpan<byte>([1]));

        mock.Object.GetBytes();
        mock.Object.GetBytes();

        mock.GetBytes().WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue(); // if we get here, verification passed
    }

    [Test]
    public async Task Span_Return_Verify_WasNeverCalled()
    {
        var mock = Mock.Of<ISpanProducer>();

        mock.GetBytes().WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Span_Return_Verify_With_Specific_Args()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes(Arg.Any<string>()).Returns(new ReadOnlySpan<byte>([1]));

        mock.Object.GetBytes("hello");
        mock.Object.GetBytes("world");

        mock.GetBytes("hello").WasCalled(Times.Once);
        mock.GetBytes("world").WasCalled(Times.Once);
        mock.GetBytes(Arg.Any<string>()).WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Span_Return_Large_Data()
    {
        var largeData = new byte[1024];
        for (int i = 0; i < largeData.Length; i++)
            largeData[i] = (byte)(i % 256);

        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes().Returns(new ReadOnlySpan<byte>(largeData));

        var result = mock.Object.GetBytes();
        var len = result.Length;
        var first = result[0];
        var last = result[1023];

        await Assert.That(len).IsEqualTo(1024);
        await Assert.That(first).IsEqualTo((byte)0);
        await Assert.That(last).IsEqualTo((byte)255);
    }

    [Test]
    public async Task Span_Return_Unmatched_Args_Returns_Default()
    {
        var mock = Mock.Of<ISpanProducer>();
        mock.GetBytes("specific").Returns(new ReadOnlySpan<byte>([1, 2, 3]));

        // Call with different arg — should return default (empty span)
        var result = mock.Object.GetBytes("other");
        var len = result.Length;

        await Assert.That(len).IsEqualTo(0);
    }
}
