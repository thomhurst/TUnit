using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// ═══════════════════════════════════════════════════════════════════════════════
// Interfaces
// ═══════════════════════════════════════════════════════════════════════════════

/// <summary>Out Span&lt;byte&gt; (mutable span) parameters.</summary>
public interface IMutableSpanOutput
{
    void Fill(out Span<byte> buffer);
    int Write(string text, out Span<byte> written);
}

/// <summary>Out ReadOnlySpan&lt;char&gt; (different element type).</summary>
public interface ICharSpanReader
{
    bool TryReadLine(out ReadOnlySpan<char> line);
    int GetToken(string input, out ReadOnlySpan<char> token);
}

/// <summary>Multiple out params: regular int + ReadOnlySpan.</summary>
public interface IMultiOutput
{
    bool Extract(string input, out int count, out ReadOnlySpan<byte> data);
}

/// <summary>Ref int + out ReadOnlySpan combo.</summary>
public interface ICodec
{
    bool Decode(ref int offset, out ReadOnlySpan<byte> decoded);
}

/// <summary>ReadOnlySpan input + ReadOnlySpan output on the same method.</summary>
public interface ITransformer
{
    void Transform(ReadOnlySpan<byte> input, out ReadOnlySpan<byte> output);
}

/// <summary>Ref with returns and multiple params.</summary>
public interface ICounter
{
    int Increment(ref int value, int step);
    void Clear(ref int value);
    bool TryAdvance(ref int position, int limit);
}

/// <summary>Ref + out (non-span) on the same method.</summary>
public interface ISwapper
{
    void SwapAndReport(ref int value, out string report);
}

/// <summary>Multiple ref struct out params of different element types.</summary>
public interface IDualSpanOutput
{
    void Split(string input, out ReadOnlySpan<byte> bytes, out ReadOnlySpan<char> chars);
}

// ═══════════════════════════════════════════════════════════════════════════════
// Out Span<byte> (mutable) tests
// ═══════════════════════════════════════════════════════════════════════════════

public class OutMutableSpanTests
{
    [Test]
    public async Task Out_Span_Empty()
    {
        var mock = Mock.Of<IMutableSpanOutput>();
        mock.Fill().SetsOutBuffer(Span<byte>.Empty);

        mock.Object.Fill(out var buffer);
        var length = buffer.Length;

        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Out_Span_With_Data()
    {
        var mock = Mock.Of<IMutableSpanOutput>();
        mock.Fill().SetsOutBuffer(new Span<byte>([10, 20, 30]));

        mock.Object.Fill(out var buffer);
        var length = buffer.Length;
        var b0 = buffer[0];
        var b1 = buffer[1];
        var b2 = buffer[2];

        await Assert.That(length).IsEqualTo(3);
        await Assert.That(b0).IsEqualTo((byte)10);
        await Assert.That(b1).IsEqualTo((byte)20);
        await Assert.That(b2).IsEqualTo((byte)30);
    }

    [Test]
    public async Task Out_Span_With_Returns_And_Mixed_Params()
    {
        var mock = Mock.Of<IMutableSpanOutput>();
        mock.Write("hello")
            .Returns(5)
            .SetsOutWritten(new Span<byte>([0x68, 0x65, 0x6C]));

        var result = mock.Object.Write("hello", out var written);
        var length = written.Length;
        var w0 = written[0];

        await Assert.That(result).IsEqualTo(5);
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(w0).IsEqualTo((byte)0x68);
    }

    [Test]
    public async Task Out_Span_Callback_Fires()
    {
        var wasCalled = false;
        var mock = Mock.Of<IMutableSpanOutput>();
        mock.Fill()
            .Callback(() => wasCalled = true)
            .SetsOutBuffer(new Span<byte>([1]));

        mock.Object.Fill(out _);

        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task Out_Span_Throws()
    {
        var mock = Mock.Of<IMutableSpanOutput>();
        mock.Fill().Throws<InvalidOperationException>();

        var ex = Assert.Throws<InvalidOperationException>(() => mock.Object.Fill(out _));

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Out_Span_Verification()
    {
        var mock = Mock.Of<IMutableSpanOutput>();
        mock.Object.Fill(out _);
        mock.Object.Fill(out _);

        mock.Fill().WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Out_Span_Never_Called()
    {
        var mock = Mock.Of<IMutableSpanOutput>();

        mock.Fill().WasNeverCalled();
        await Assert.That(true).IsTrue();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Out ReadOnlySpan<char> tests
// ═══════════════════════════════════════════════════════════════════════════════

public class OutReadOnlySpanCharTests
{
    [Test]
    public async Task Out_ReadOnlySpan_Char_With_Data()
    {
        var mock = Mock.Of<ICharSpanReader>();
        mock.TryReadLine()
            .Returns(true)
            .SetsOutLine("hello world".AsSpan());

        var success = mock.Object.TryReadLine(out var line);
        var length = line.Length;
        var c0 = line[0];
        var c4 = line[4];

        await Assert.That(success).IsTrue();
        await Assert.That(length).IsEqualTo(11);
        await Assert.That(c0).IsEqualTo('h');
        await Assert.That(c4).IsEqualTo('o');
    }

    [Test]
    public async Task Out_ReadOnlySpan_Char_Empty()
    {
        var mock = Mock.Of<ICharSpanReader>();
        mock.TryReadLine()
            .Returns(false)
            .SetsOutLine(ReadOnlySpan<char>.Empty);

        var success = mock.Object.TryReadLine(out var line);
        var length = line.Length;

        await Assert.That(success).IsFalse();
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Out_ReadOnlySpan_Char_Mixed_Params_With_Matchers()
    {
        var mock = Mock.Of<ICharSpanReader>();
        mock.GetToken("CSV")
            .Returns(3)
            .SetsOutToken("foo".AsSpan());

        mock.GetToken("JSON")
            .Returns(4)
            .SetsOutToken("data".AsSpan());

        // First call
        var r1 = mock.Object.GetToken("CSV", out var t1);
        var t1Len = t1.Length;
        var t1c0 = t1[0];

        // Second call
        var r2 = mock.Object.GetToken("JSON", out var t2);
        var t2Len = t2.Length;
        var t2c0 = t2[0];

        await Assert.That(r1).IsEqualTo(3);
        await Assert.That(t1Len).IsEqualTo(3);
        await Assert.That(t1c0).IsEqualTo('f');
        await Assert.That(r2).IsEqualTo(4);
        await Assert.That(t2Len).IsEqualTo(4);
        await Assert.That(t2c0).IsEqualTo('d');
    }

    [Test]
    public async Task Out_ReadOnlySpan_Char_Arg_Any_Matcher()
    {
        var mock = Mock.Of<ICharSpanReader>();
        mock.GetToken(Arg.Any<string>())
            .Returns(1)
            .SetsOutToken("x".AsSpan());

        var r1 = mock.Object.GetToken("anything", out var t1);
        var r2 = mock.Object.GetToken("else", out var t2);
        var t1Len = t1.Length;
        var t2Len = t2.Length;

        await Assert.That(r1).IsEqualTo(1);
        await Assert.That(r2).IsEqualTo(1);
        await Assert.That(t1Len).IsEqualTo(1);
        await Assert.That(t2Len).IsEqualTo(1);
    }

    [Test]
    public async Task Out_ReadOnlySpan_Char_Arg_Is_Predicate()
    {
        var mock = Mock.Of<ICharSpanReader>();
        mock.GetToken(Arg.Is<string>(s => s.StartsWith("J")))
            .Returns(42)
            .SetsOutToken("json".AsSpan());

        var r1 = mock.Object.GetToken("JSON", out var t1);
        var t1Len = t1.Length;
        var r2 = mock.Object.GetToken("CSV", out var t2);
        var t2Len = t2.Length;

        await Assert.That(r1).IsEqualTo(42);
        await Assert.That(t1Len).IsEqualTo(4);
        // "CSV" doesn't match predicate, returns default
        await Assert.That(r2).IsEqualTo(0);
        await Assert.That(t2Len).IsEqualTo(0);
    }

    [Test]
    public async Task Out_ReadOnlySpan_Char_Verification_Multiple()
    {
        var mock = Mock.Of<ICharSpanReader>();
        mock.GetToken(Arg.Any<string>()).Returns(0);

        mock.Object.GetToken("a", out _);
        mock.Object.GetToken("b", out _);
        mock.Object.GetToken("a", out _);

        mock.GetToken("a").WasCalled(Times.Exactly(2));
        mock.GetToken("b").WasCalled(Times.Once);
        mock.GetToken(Arg.Any<string>()).WasCalled(Times.Exactly(3));
        mock.GetToken("z").WasNeverCalled();
        await Assert.That(true).IsTrue();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Multiple out params (int + ReadOnlySpan) tests
// ═══════════════════════════════════════════════════════════════════════════════

public class MultipleOutParamsTests
{
    [Test]
    public async Task Multiple_Out_Int_And_Span()
    {
        var mock = Mock.Of<IMultiOutput>();
        mock.Extract("test")
            .Returns(true)
            .SetsOutCount(3)
            .SetsOutData(new ReadOnlySpan<byte>([0xAA, 0xBB, 0xCC]));

        var success = mock.Object.Extract("test", out var count, out var data);
        var dataLen = data.Length;
        var d0 = data[0];
        var d2 = data[2];

        await Assert.That(success).IsTrue();
        await Assert.That(count).IsEqualTo(3);
        await Assert.That(dataLen).IsEqualTo(3);
        await Assert.That(d0).IsEqualTo((byte)0xAA);
        await Assert.That(d2).IsEqualTo((byte)0xCC);
    }

    [Test]
    public async Task Multiple_Out_Only_Int_Set()
    {
        // Only set the int out param, span stays default
        var mock = Mock.Of<IMultiOutput>();
        mock.Extract("partial")
            .Returns(true)
            .SetsOutCount(7);

        var success = mock.Object.Extract("partial", out var count, out var data);
        var dataLen = data.Length;

        await Assert.That(success).IsTrue();
        await Assert.That(count).IsEqualTo(7);
        await Assert.That(dataLen).IsEqualTo(0);
    }

    [Test]
    public async Task Multiple_Out_Only_Span_Set()
    {
        // Only set the span out param, int stays default
        var mock = Mock.Of<IMultiOutput>();
        mock.Extract("data-only")
            .Returns(false)
            .SetsOutData(new ReadOnlySpan<byte>([1, 2]));

        var success = mock.Object.Extract("data-only", out var count, out var data);
        var dataLen = data.Length;

        await Assert.That(success).IsFalse();
        await Assert.That(count).IsEqualTo(0); // default int
        await Assert.That(dataLen).IsEqualTo(2);
    }

    [Test]
    public async Task Multiple_Out_Chain_Order_SetsOut_Before_Returns()
    {
        var mock = Mock.Of<IMultiOutput>();
        mock.Extract(Arg.Any<string>())
            .SetsOutCount(99)
            .SetsOutData(new ReadOnlySpan<byte>([0xFF]))
            .Returns(true);

        var success = mock.Object.Extract("any", out var count, out var data);
        var dataLen = data.Length;

        await Assert.That(success).IsTrue();
        await Assert.That(count).IsEqualTo(99);
        await Assert.That(dataLen).IsEqualTo(1);
    }

    [Test]
    public async Task Multiple_Out_Different_Setups_Per_Input()
    {
        var mock = Mock.Of<IMultiOutput>();
        mock.Extract("alpha")
            .Returns(true)
            .SetsOutCount(1)
            .SetsOutData(new ReadOnlySpan<byte>([0x01]));

        mock.Extract("beta")
            .Returns(true)
            .SetsOutCount(2)
            .SetsOutData(new ReadOnlySpan<byte>([0x02, 0x03]));

        // alpha
        mock.Object.Extract("alpha", out var c1, out var d1);
        var c1Val = c1;
        var d1Len = d1.Length;
        var d1b0 = d1[0];

        // beta
        mock.Object.Extract("beta", out var c2, out var d2);
        var c2Val = c2;
        var d2Len = d2.Length;

        await Assert.That(c1Val).IsEqualTo(1);
        await Assert.That(d1Len).IsEqualTo(1);
        await Assert.That(d1b0).IsEqualTo((byte)0x01);
        await Assert.That(c2Val).IsEqualTo(2);
        await Assert.That(d2Len).IsEqualTo(2);
    }

    [Test]
    public async Task Multiple_Out_Callback_With_Args()
    {
        string? capturedInput = null;
        var mock = Mock.Of<IMultiOutput>();
        mock.Extract(Arg.Any<string>())
            .Callback((Action<object?[]>)(args => capturedInput = (string?)args[0]))
            .Returns(true)
            .SetsOutCount(1)
            .SetsOutData(new ReadOnlySpan<byte>([0xDE]));

        mock.Object.Extract("captured", out _, out _);

        await Assert.That(capturedInput).IsEqualTo("captured");
    }

    [Test]
    public async Task Multiple_Out_Throws_Exception()
    {
        var mock = Mock.Of<IMultiOutput>();
        mock.Extract("bad").Throws<ArgumentException>();

        var ex = Assert.Throws<ArgumentException>(() =>
            mock.Object.Extract("bad", out _, out _));

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Multiple_Out_Verification()
    {
        var mock = Mock.Of<IMultiOutput>();
        mock.Extract(Arg.Any<string>()).Returns(false);

        mock.Object.Extract("a", out _, out _);
        mock.Object.Extract("b", out _, out _);
        mock.Object.Extract("a", out _, out _);

        mock.Extract("a").WasCalled(Times.Exactly(2));
        mock.Extract("b").WasCalled(Times.Once);
        mock.Extract(Arg.Any<string>()).WasCalled(Times.Exactly(3));
        mock.Extract("c").WasNeverCalled();
        await Assert.That(true).IsTrue();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Ref + out span combo tests
// ═══════════════════════════════════════════════════════════════════════════════

public class RefAndOutSpanTests
{
    [Test]
    public async Task Ref_Int_And_Out_Span()
    {
        var mock = Mock.Of<ICodec>();
        mock.Decode(0)
            .Returns(true)
            .SetsRefOffset(10)
            .SetsOutDecoded(new ReadOnlySpan<byte>([0xCA, 0xFE]));

        int offset = 0;
        var success = mock.Object.Decode(ref offset, out var decoded);
        var decodedLen = decoded.Length;
        var d0 = decoded[0];
        var d1 = decoded[1];

        await Assert.That(success).IsTrue();
        await Assert.That(offset).IsEqualTo(10);
        await Assert.That(decodedLen).IsEqualTo(2);
        await Assert.That(d0).IsEqualTo((byte)0xCA);
        await Assert.That(d1).IsEqualTo((byte)0xFE);
    }

    [Test]
    public async Task Ref_Int_And_Out_Span_Different_Offsets()
    {
        var mock = Mock.Of<ICodec>();
        mock.Decode(0)
            .Returns(true)
            .SetsRefOffset(5)
            .SetsOutDecoded(new ReadOnlySpan<byte>([1, 2, 3, 4, 5]));

        mock.Decode(5)
            .Returns(true)
            .SetsRefOffset(8)
            .SetsOutDecoded(new ReadOnlySpan<byte>([6, 7, 8]));

        mock.Decode(8)
            .Returns(false)
            .SetsRefOffset(8);

        // First decode
        int pos = 0;
        var r1 = mock.Object.Decode(ref pos, out var d1);
        var d1Len = d1.Length;

        // Second decode
        var r2 = mock.Object.Decode(ref pos, out var d2);
        var d2Len = d2.Length;

        // Third decode (end)
        var r3 = mock.Object.Decode(ref pos, out var d3);
        var d3Len = d3.Length;

        await Assert.That(r1).IsTrue();
        await Assert.That(pos).IsEqualTo(8); // modified by second decode
        await Assert.That(d1Len).IsEqualTo(5);
        await Assert.That(r2).IsTrue();
        await Assert.That(d2Len).IsEqualTo(3);
        await Assert.That(r3).IsFalse();
        await Assert.That(d3Len).IsEqualTo(0);
    }

    [Test]
    public async Task Ref_And_Out_Span_With_Any_Matcher()
    {
        var mock = Mock.Of<ICodec>();
        mock.Decode(Arg.Any<int>())
            .Returns(true)
            .SetsRefOffset(100)
            .SetsOutDecoded(new ReadOnlySpan<byte>([0xFF]));

        int pos = 42;
        var success = mock.Object.Decode(ref pos, out var decoded);
        var dLen = decoded.Length;

        await Assert.That(success).IsTrue();
        await Assert.That(pos).IsEqualTo(100);
        await Assert.That(dLen).IsEqualTo(1);
    }

    [Test]
    public async Task Ref_And_Out_Span_Verification()
    {
        var mock = Mock.Of<ICodec>();
        mock.Decode(Arg.Any<int>()).Returns(false);

        int p1 = 0, p2 = 5;
        mock.Object.Decode(ref p1, out _);
        mock.Object.Decode(ref p2, out _);

        mock.Decode(0).WasCalled(Times.Once);
        mock.Decode(5).WasCalled(Times.Once);
        mock.Decode(Arg.Any<int>()).WasCalled(Times.Exactly(2));
        mock.Decode(99).WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Ref_And_Out_Span_Throws()
    {
        var mock = Mock.Of<ICodec>();
        mock.Decode(-1).Throws<ArgumentOutOfRangeException>();

        int bad = -1;
        var ex = Assert.Throws<ArgumentOutOfRangeException>(() =>
            mock.Object.Decode(ref bad, out _));

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Ref_And_Out_Span_Callback()
    {
        var wasCalled = false;
        var mock = Mock.Of<ICodec>();
        mock.Decode(Arg.Any<int>())
            .Callback(() => wasCalled = true)
            .Returns(true)
            .SetsOutDecoded(new ReadOnlySpan<byte>([1]));

        int pos = 0;
        mock.Object.Decode(ref pos, out _);

        await Assert.That(wasCalled).IsTrue();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ReadOnlySpan input + ReadOnlySpan output tests
// ═══════════════════════════════════════════════════════════════════════════════

public class SpanInputAndOutputTests
{
#if NET9_0_OR_GREATER
    [Test]
    public async Task RefStructArg_Input_With_Out_Span()
    {
        var mock = Mock.Of<ITransformer>();
        mock.Transform(RefStructArg<ReadOnlySpan<byte>>.Any)
            .SetsOutOutput(new ReadOnlySpan<byte>([0xDE, 0xAD]));

        mock.Object.Transform(new byte[] { 1, 2, 3 }, out var output);
        var len = output.Length;
        var o0 = output[0];
        var o1 = output[1];

        await Assert.That(len).IsEqualTo(2);
        await Assert.That(o0).IsEqualTo((byte)0xDE);
        await Assert.That(o1).IsEqualTo((byte)0xAD);
    }

    [Test]
    public async Task RefStructArg_Input_With_Out_Span_Callback()
    {
        var wasCalled = false;
        var mock = Mock.Of<ITransformer>();
        mock.Transform(RefStructArg<ReadOnlySpan<byte>>.Any)
            .Callback(() => wasCalled = true)
            .SetsOutOutput(new ReadOnlySpan<byte>([1]));

        mock.Object.Transform(new byte[] { 0xFF }, out _);

        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task RefStructArg_Input_With_Out_Span_Throws()
    {
        var mock = Mock.Of<ITransformer>();
        mock.Transform(RefStructArg<ReadOnlySpan<byte>>.Any)
            .Throws<NotSupportedException>();

        var ex = Assert.Throws<NotSupportedException>(() =>
            mock.Object.Transform(new byte[] { 1 }, out _));

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task RefStructArg_Input_With_Out_Span_Verification()
    {
        var mock = Mock.Of<ITransformer>();
        mock.Object.Transform(new byte[] { 1 }, out _);
        mock.Object.Transform(ReadOnlySpan<byte>.Empty, out _);

        mock.Transform(RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }
#else
    [Test]
    public async Task PreNet9_Span_Input_With_Out_Span()
    {
        // Pre-NET9: ref struct input excluded from matching
        var mock = Mock.Of<ITransformer>();
        mock.Transform()
            .SetsOutOutput(new ReadOnlySpan<byte>([0xBE, 0xEF]));

        mock.Object.Transform(new byte[] { 1 }, out var output);
        var len = output.Length;
        var o0 = output[0];

        await Assert.That(len).IsEqualTo(2);
        await Assert.That(o0).IsEqualTo((byte)0xBE);
    }

    [Test]
    public async Task PreNet9_Span_Input_Verification()
    {
        var mock = Mock.Of<ITransformer>();
        mock.Object.Transform(new byte[] { 1 }, out _);
        mock.Object.Transform(ReadOnlySpan<byte>.Empty, out _);

        mock.Transform().WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }
#endif
}

// ═══════════════════════════════════════════════════════════════════════════════
// Comprehensive ref parameter tests
// ═══════════════════════════════════════════════════════════════════════════════

public class ComprehensiveRefTests
{
    [Test]
    public async Task Ref_With_Return_Value()
    {
        var mock = Mock.Of<ICounter>();
        mock.Increment(Arg.Any<int>(), 1)
            .Returns(1)
            .SetsRefValue(11);

        int val = 10;
        var result = mock.Object.Increment(ref val, 1);

        await Assert.That(result).IsEqualTo(1);
        await Assert.That(val).IsEqualTo(11);
    }

    [Test]
    public async Task Ref_Exact_Value_Matching()
    {
        var mock = Mock.Of<ICounter>();
        mock.Increment(10, 1).Returns(11).SetsRefValue(11);
        mock.Increment(20, 1).Returns(21).SetsRefValue(21);

        int v1 = 10;
        var r1 = mock.Object.Increment(ref v1, 1);

        int v2 = 20;
        var r2 = mock.Object.Increment(ref v2, 1);

        await Assert.That(r1).IsEqualTo(11);
        await Assert.That(v1).IsEqualTo(11);
        await Assert.That(r2).IsEqualTo(21);
        await Assert.That(v2).IsEqualTo(21);
    }

    [Test]
    public async Task Ref_Predicate_Matching()
    {
        var mock = Mock.Of<ICounter>();
        mock.TryAdvance(Arg.Is<int>(v => v >= 0), 100)
            .Returns(true)
            .SetsRefPosition(50);

        mock.TryAdvance(Arg.Is<int>(v => v < 0), Arg.Any<int>())
            .Returns(false);

        // Positive position
        int pos = 0;
        var r1 = mock.Object.TryAdvance(ref pos, 100);
        await Assert.That(r1).IsTrue();
        await Assert.That(pos).IsEqualTo(50);

        // Negative position
        int negPos = -1;
        var r2 = mock.Object.TryAdvance(ref negPos, 100);
        await Assert.That(r2).IsFalse();
    }

    [Test]
    public async Task Ref_Void_Method()
    {
        var mock = Mock.Of<ICounter>();
        mock.Clear(Arg.Any<int>()).SetsRefValue(0);

        int val = 42;
        mock.Object.Clear(ref val);

        await Assert.That(val).IsEqualTo(0);
    }

    [Test]
    public async Task Ref_Not_Modified_Without_Setup()
    {
        var mock = Mock.Of<ICounter>();
        // No SetsRefValue configured

        int val = 42;
        mock.Object.Clear(ref val);

        // Value stays unchanged when no setter is configured
        await Assert.That(val).IsEqualTo(42);
    }

    [Test]
    public async Task Ref_Callback_Fires()
    {
        var wasCalled = false;
        var mock = Mock.Of<ICounter>();
        mock.Clear(Arg.Any<int>())
            .Callback(() => wasCalled = true)
            .SetsRefValue(0);

        int val = 10;
        mock.Object.Clear(ref val);

        await Assert.That(wasCalled).IsTrue();
        await Assert.That(val).IsEqualTo(0);
    }

    [Test]
    public async Task Ref_Throws()
    {
        var mock = Mock.Of<ICounter>();
        mock.Increment(Arg.Any<int>(), 0).Throws<ArgumentException>();

        int val = 1;
        var ex = Assert.Throws<ArgumentException>(() => mock.Object.Increment(ref val, 0));

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Ref_Verification_With_Exact_Value()
    {
        var mock = Mock.Of<ICounter>();
        mock.Increment(Arg.Any<int>(), Arg.Any<int>()).Returns(0);

        int v1 = 5, v2 = 10;
        mock.Object.Increment(ref v1, 1);
        mock.Object.Increment(ref v2, 2);
        mock.Object.Increment(ref v1, 1);

        mock.Increment(5, 1).WasCalled(Times.Exactly(2));
        mock.Increment(10, 2).WasCalled(Times.Once);
        mock.Increment(Arg.Any<int>(), Arg.Any<int>()).WasCalled(Times.Exactly(3));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Ref_Verification_AtLeast_AtMost()
    {
        var mock = Mock.Of<ICounter>();
        mock.Clear(Arg.Any<int>());

        int v = 1;
        mock.Object.Clear(ref v);
        mock.Object.Clear(ref v);
        mock.Object.Clear(ref v);

        mock.Clear(Arg.Any<int>()).WasCalled(Times.AtLeast(2));
        mock.Clear(Arg.Any<int>()).WasCalled(Times.AtMost(5));
        mock.Clear(Arg.Any<int>()).WasCalled(Times.Between(2, 4));
        await Assert.That(true).IsTrue();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Ref + out (non-span) combined tests
// ═══════════════════════════════════════════════════════════════════════════════

public class RefAndOutCombinedTests
{
    [Test]
    public async Task Ref_And_Out_Both_Set()
    {
        var mock = Mock.Of<ISwapper>();
        mock.SwapAndReport(42)
            .SetsRefValue(0)
            .SetsOutReport("swapped 42 to 0");

        int val = 42;
        mock.Object.SwapAndReport(ref val, out var report);

        await Assert.That(val).IsEqualTo(0);
        await Assert.That(report).IsEqualTo("swapped 42 to 0");
    }

    [Test]
    public async Task Ref_And_Out_Only_Ref_Set()
    {
        var mock = Mock.Of<ISwapper>();
        mock.SwapAndReport(Arg.Any<int>()).SetsRefValue(99);

        int val = 1;
        mock.Object.SwapAndReport(ref val, out var report);

        await Assert.That(val).IsEqualTo(99);
        await Assert.That(report).IsNull();
    }

    [Test]
    public async Task Ref_And_Out_Only_Out_Set()
    {
        var mock = Mock.Of<ISwapper>();
        mock.SwapAndReport(Arg.Any<int>()).SetsOutReport("report");

        int val = 50;
        mock.Object.SwapAndReport(ref val, out var report);

        // ref not configured → stays unchanged
        await Assert.That(val).IsEqualTo(50);
        await Assert.That(report).IsEqualTo("report");
    }

    [Test]
    public async Task Ref_And_Out_Callback()
    {
        var wasCalled = false;
        var mock = Mock.Of<ISwapper>();
        mock.SwapAndReport(Arg.Any<int>())
            .Callback(() => wasCalled = true)
            .SetsRefValue(0)
            .SetsOutReport("done");

        int val = 10;
        mock.Object.SwapAndReport(ref val, out _);

        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task Ref_And_Out_Throws()
    {
        var mock = Mock.Of<ISwapper>();
        mock.SwapAndReport(-1).Throws<ArgumentException>();

        int val = -1;
        var ex = Assert.Throws<ArgumentException>(() =>
            mock.Object.SwapAndReport(ref val, out _));

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Ref_And_Out_Verification()
    {
        var mock = Mock.Of<ISwapper>();

        int v1 = 1, v2 = 2;
        mock.Object.SwapAndReport(ref v1, out _);
        mock.Object.SwapAndReport(ref v2, out _);
        mock.Object.SwapAndReport(ref v1, out _);

        mock.SwapAndReport(1).WasCalled(Times.Exactly(2));
        mock.SwapAndReport(2).WasCalled(Times.Once);
        mock.SwapAndReport(Arg.Any<int>()).WasCalled(Times.Exactly(3));
        await Assert.That(true).IsTrue();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Multiple ref struct out params of different element types
// ═══════════════════════════════════════════════════════════════════════════════

public class DualSpanOutputTests
{
    [Test]
    public async Task Two_Span_Out_Params_Both_Set()
    {
        var mock = Mock.Of<IDualSpanOutput>();
        mock.Split("hello")
            .SetsOutBytes(new ReadOnlySpan<byte>([0x68, 0x65]))
            .SetsOutChars("hi".AsSpan());

        mock.Object.Split("hello", out var bytes, out var chars);
        var bLen = bytes.Length;
        var cLen = chars.Length;
        var b0 = bytes[0];
        var c0 = chars[0];

        await Assert.That(bLen).IsEqualTo(2);
        await Assert.That(cLen).IsEqualTo(2);
        await Assert.That(b0).IsEqualTo((byte)0x68);
        await Assert.That(c0).IsEqualTo('h');
    }

    [Test]
    public async Task Two_Span_Out_Only_First_Set()
    {
        var mock = Mock.Of<IDualSpanOutput>();
        mock.Split("partial")
            .SetsOutBytes(new ReadOnlySpan<byte>([1, 2, 3]));

        mock.Object.Split("partial", out var bytes, out var chars);
        var bLen = bytes.Length;
        var cLen = chars.Length;

        await Assert.That(bLen).IsEqualTo(3);
        await Assert.That(cLen).IsEqualTo(0); // default
    }

    [Test]
    public async Task Two_Span_Out_Only_Second_Set()
    {
        var mock = Mock.Of<IDualSpanOutput>();
        mock.Split("chars-only")
            .SetsOutChars("abc".AsSpan());

        mock.Object.Split("chars-only", out var bytes, out var chars);
        var bLen = bytes.Length;
        var cLen = chars.Length;
        var c2 = chars[2];

        await Assert.That(bLen).IsEqualTo(0); // default
        await Assert.That(cLen).IsEqualTo(3);
        await Assert.That(c2).IsEqualTo('c');
    }

    [Test]
    public async Task Two_Span_Out_Verification()
    {
        var mock = Mock.Of<IDualSpanOutput>();

        mock.Object.Split("a", out _, out _);
        mock.Object.Split("b", out _, out _);

        mock.Split("a").WasCalled(Times.Once);
        mock.Split("b").WasCalled(Times.Once);
        mock.Split(Arg.Any<string>()).WasCalled(Times.Exactly(2));
        mock.Split("c").WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Two_Span_Out_Throws()
    {
        var mock = Mock.Of<IDualSpanOutput>();
        mock.Split("").Throws<ArgumentException>();

        var ex = Assert.Throws<ArgumentException>(() =>
            mock.Object.Split("", out _, out _));

        await Assert.That(ex).IsNotNull();
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// Sequential and advanced setup patterns with out span params
// ═══════════════════════════════════════════════════════════════════════════════

public class SequentialSpanSetupTests
{
    [Test]
    public async Task Then_Returns_Sequence_With_Out_Span()
    {
        // SetsOut applies at setup level; Then() sequences Returns/Callback/Throws
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse(Arg.Any<string>())
            .SetsOutData(new ReadOnlySpan<byte>([0xAA]))
            .Returns(true)
            .Then()
            .Returns(false);

        // First call: returns true, span data set
        var r1 = mock.Object.TryParse("a", out var d1);
        var d1Len = d1.Length;
        var d1b0 = d1[0];

        // Second call: returns false (Then sequencing), span data still set
        var r2 = mock.Object.TryParse("b", out var d2);
        var d2Len = d2.Length;

        await Assert.That(r1).IsTrue();
        await Assert.That(d1Len).IsEqualTo(1);
        await Assert.That(d1b0).IsEqualTo((byte)0xAA);
        await Assert.That(r2).IsFalse();
        await Assert.That(d2Len).IsEqualTo(1); // span still applied
    }

    [Test]
    public async Task Then_Throws_After_Success_With_Out_Span()
    {
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse("data")
            .Returns(true)
            .SetsOutData(new ReadOnlySpan<byte>([0xAA]))
            .Then()
            .Throws<InvalidOperationException>();

        // First call succeeds with span data
        var r1 = mock.Object.TryParse("data", out var d1);
        var d1Len = d1.Length;

        // Second call throws
        var ex = Assert.Throws<InvalidOperationException>(() =>
            mock.Object.TryParse("data", out _));

        await Assert.That(r1).IsTrue();
        await Assert.That(d1Len).IsEqualTo(1);
        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Then_Callback_Sequence_With_Out_Span()
    {
        // Callbacks sequence correctly with Then(), span stays constant
        var callCount = 0;
        var mock = Mock.Of<ISpanWriter>();
        mock.Do()
            .Callback(() => callCount++)
            .SetsOutBuffer(new ReadOnlySpan<byte>([0xFF]))
            .Then()
            .Callback(() => callCount += 10);

        mock.Object.Do(out var d1);
        var d1b0 = d1[0];
        mock.Object.Do(out var d2);
        var d2b0 = d2[0];

        await Assert.That(callCount).IsEqualTo(11);
        // Both calls get the same span data
        await Assert.That(d1b0).IsEqualTo((byte)0xFF);
        await Assert.That(d2b0).IsEqualTo((byte)0xFF);
    }

    [Test]
    public async Task ReturnsSequentially_With_Out_Span()
    {
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse(Arg.Any<string>())
            .ReturnsSequentially(true, true, false)
            .SetsOutData(new ReadOnlySpan<byte>([0x01, 0x02]));

        var r1 = mock.Object.TryParse("a", out var d1);
        var d1Len = d1.Length;
        var r2 = mock.Object.TryParse("b", out var d2);
        var d2Len = d2.Length;
        var r3 = mock.Object.TryParse("c", out var d3);
        var d3Len = d3.Length;

        await Assert.That(r1).IsTrue();
        await Assert.That(r2).IsTrue();
        await Assert.That(r3).IsFalse();
        // All calls get the same span data
        await Assert.That(d1Len).IsEqualTo(2);
        await Assert.That(d2Len).IsEqualTo(2);
        await Assert.That(d3Len).IsEqualTo(2);
    }

    [Test]
    public async Task Separate_Setups_For_Different_Span_Data()
    {
        // Use separate setups per input value instead of Then() for different span data
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse("first")
            .Returns(true)
            .SetsOutData(new ReadOnlySpan<byte>([0x01]));

        mock.TryParse("second")
            .Returns(true)
            .SetsOutData(new ReadOnlySpan<byte>([0x02, 0x03]));

        var r1 = mock.Object.TryParse("first", out var d1);
        var d1Len = d1.Length;
        var d1b0 = d1[0];

        var r2 = mock.Object.TryParse("second", out var d2);
        var d2Len = d2.Length;
        var d2b0 = d2[0];

        await Assert.That(r1).IsTrue();
        await Assert.That(d1Len).IsEqualTo(1);
        await Assert.That(d1b0).IsEqualTo((byte)0x01);
        await Assert.That(r2).IsTrue();
        await Assert.That(d2Len).IsEqualTo(2);
        await Assert.That(d2b0).IsEqualTo((byte)0x02);
    }
}

// ═══════════════════════════════════════════════════════════════════════════════
// ReadOnlySpan<byte> comprehensive out parameter tests (extends OutRefSpanTests)
// ═══════════════════════════════════════════════════════════════════════════════

public class ReadOnlySpanByteOutComprehensiveTests
{
    [Test]
    public async Task Callback_With_Void_Out_Span_Method()
    {
        var wasCalled = false;
        var mock = Mock.Of<ISpanWriter>();
        mock.Do()
            .Callback(() => wasCalled = true)
            .SetsOutBuffer(new ReadOnlySpan<byte>([0xFF]));

        mock.Object.Do(out var buffer);
        var len = buffer.Length;

        await Assert.That(wasCalled).IsTrue();
        await Assert.That(len).IsEqualTo(1);
    }

    [Test]
    public async Task Throws_With_Void_Out_Span_Method()
    {
        var mock = Mock.Of<ISpanWriter>();
        mock.Do().Throws<NotImplementedException>();

        var ex = Assert.Throws<NotImplementedException>(() =>
            mock.Object.Do(out _));

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task Throws_Instance_With_Out_Span()
    {
        var mock = Mock.Of<ISpanWriter>();
        mock.Do().Throws(new InvalidOperationException("custom message"));

        var ex = Assert.Throws<InvalidOperationException>(() =>
            mock.Object.Do(out _));

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.Message).IsEqualTo("custom message");
    }

    [Test]
    public async Task Verification_Once()
    {
        var mock = Mock.Of<ISpanWriter>();
        mock.Object.Do(out _);

        mock.Do().WasCalled(Times.Once);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verification_Multiple()
    {
        var mock = Mock.Of<ISpanWriter>();
        mock.Object.Do(out _);
        mock.Object.Do(out _);
        mock.Object.Do(out _);

        mock.Do().WasCalled(Times.Exactly(3));
        mock.Do().WasCalled(Times.AtLeast(2));
        mock.Do().WasCalled(Times.AtMost(5));
        mock.Do().WasCalled(Times.Between(1, 4));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Verification_Never()
    {
        var mock = Mock.Of<ISpanWriter>();

        mock.Do().WasNeverCalled();
        mock.Do().WasCalled(Times.Never);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Out_Span_Large_Data()
    {
        var largeData = new byte[1024];
        for (int i = 0; i < largeData.Length; i++)
            largeData[i] = (byte)(i % 256);

        var mock = Mock.Of<ISpanWriter>();
        mock.Do().SetsOutBuffer(new ReadOnlySpan<byte>(largeData));

        mock.Object.Do(out var buffer);
        var len = buffer.Length;
        var first = buffer[0];
        var last = buffer[1023];

        await Assert.That(len).IsEqualTo(1024);
        await Assert.That(first).IsEqualTo((byte)0);
        await Assert.That(last).IsEqualTo((byte)(1023 % 256));
    }

    [Test]
    public async Task Out_Span_Chaining_Returns_Then_SetsOut()
    {
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse("a")
            .Returns(true)
            .SetsOutData(new ReadOnlySpan<byte>([1]));

        var success = mock.Object.TryParse("a", out var data);
        var len = data.Length;

        await Assert.That(success).IsTrue();
        await Assert.That(len).IsEqualTo(1);
    }

    [Test]
    public async Task Out_Span_Chaining_SetsOut_Then_Returns()
    {
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse("b")
            .SetsOutData(new ReadOnlySpan<byte>([2, 3]))
            .Returns(true);

        var success = mock.Object.TryParse("b", out var data);
        var len = data.Length;

        await Assert.That(success).IsTrue();
        await Assert.That(len).IsEqualTo(2);
    }

    [Test]
    public async Task Out_Span_Untyped_SetsOutParameter_Still_Works()
    {
        // Backward compat: use index-based API with a byte array
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse("key")
            .Returns(true)
            .SetsOutParameter(1, new byte[] { 0xDE, 0xAD });

        var success = mock.Object.TryParse("key", out var data);
        var len = data.Length;
        var d0 = data[0];

        await Assert.That(success).IsTrue();
        await Assert.That(len).IsEqualTo(2);
        await Assert.That(d0).IsEqualTo((byte)0xDE);
    }

    [Test]
    public async Task Out_Span_Different_Values_Per_Input()
    {
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse("alpha")
            .Returns(true)
            .SetsOutData(new ReadOnlySpan<byte>([0x01]));

        mock.TryParse("beta")
            .Returns(true)
            .SetsOutData(new ReadOnlySpan<byte>([0x02, 0x03]));

        mock.TryParse("gamma")
            .Returns(false);

        // alpha
        var r1 = mock.Object.TryParse("alpha", out var d1);
        var d1Len = d1.Length;

        // beta
        var r2 = mock.Object.TryParse("beta", out var d2);
        var d2Len = d2.Length;

        // gamma — no span set
        var r3 = mock.Object.TryParse("gamma", out var d3);
        var d3Len = d3.Length;

        // unknown — no setup at all
        var r4 = mock.Object.TryParse("unknown", out var d4);
        var d4Len = d4.Length;

        await Assert.That(r1).IsTrue();
        await Assert.That(d1Len).IsEqualTo(1);
        await Assert.That(r2).IsTrue();
        await Assert.That(d2Len).IsEqualTo(2);
        await Assert.That(r3).IsFalse();
        await Assert.That(d3Len).IsEqualTo(0);
        await Assert.That(r4).IsFalse(); // default
        await Assert.That(d4Len).IsEqualTo(0);
    }

    [Test]
    public async Task Out_Span_Verification_With_Mixed_Params()
    {
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse(Arg.Any<string>()).Returns(false);

        mock.Object.TryParse("x", out _);
        mock.Object.TryParse("y", out _);
        mock.Object.TryParse("x", out _);

        mock.TryParse("x").WasCalled(Times.Exactly(2));
        mock.TryParse("y").WasCalled(Times.Once);
        mock.TryParse(Arg.Any<string>()).WasCalled(Times.Exactly(3));
        mock.TryParse("z").WasNeverCalled();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Out_Span_Callback_With_Args()
    {
        string? capturedInput = null;
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse(Arg.Any<string>())
            .Callback((Action<object?[]>)(args => capturedInput = (string?)args[0]))
            .Returns(true)
            .SetsOutData(new ReadOnlySpan<byte>([1]));

        mock.Object.TryParse("captured-input", out _);

        await Assert.That(capturedInput).IsEqualTo("captured-input");
    }

    [Test]
    public async Task Out_Span_Throws_Exception_Factory()
    {
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse(Arg.Any<string>())
            .Throws((Func<object?[], Exception>)(args =>
                new ArgumentException($"Bad input: {args[0]}")));

        var ex = Assert.Throws<ArgumentException>(() =>
            mock.Object.TryParse("bad", out _));

        await Assert.That(ex).IsNotNull();
        await Assert.That(ex!.Message).IsEqualTo("Bad input: bad");
    }
}
