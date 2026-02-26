using TUnit.Mocks;

namespace TUnit.Mocks.Tests;

// ─── Interfaces with out/ref span parameters ────────────────────────────────

public interface ISpanWriter
{
    void Do(out ReadOnlySpan<byte> buffer);
}

public interface ISpanParser
{
    bool TryParse(string input, out ReadOnlySpan<byte> data);
}

// ─── Tests ──────────────────────────────────────────────────────────────────

/// <summary>
/// Tests for out parameters of ReadOnlySpan/Span types, which are ref structs
/// and require array-based conversion for storage.
/// </summary>
public class OutRefSpanTests
{
    [Test]
    public async Task Out_ReadOnlySpan_Default_Works()
    {
        // Arrange
        var mock = Mock.Of<ISpanWriter>();
        mock.Do().SetsOutBuffer(new ReadOnlySpan<byte>());

        // Act
        mock.Object.Do(out var buffer);
        var length = buffer.Length;

        // Assert — empty span
        await Assert.That(length).IsEqualTo(0);
    }

    [Test]
    public async Task Out_ReadOnlySpan_With_Data()
    {
        // Arrange
        var mock = Mock.Of<ISpanWriter>();
        mock.Do().SetsOutBuffer(new ReadOnlySpan<byte>([1, 2, 3]));

        // Act
        mock.Object.Do(out var buffer);
        var length = buffer.Length;
        var b0 = buffer[0];
        var b1 = buffer[1];
        var b2 = buffer[2];

        // Assert
        await Assert.That(length).IsEqualTo(3);
        await Assert.That(b0).IsEqualTo((byte)1);
        await Assert.That(b1).IsEqualTo((byte)2);
        await Assert.That(b2).IsEqualTo((byte)3);
    }

    [Test]
    public async Task Out_ReadOnlySpan_Mixed_Params_With_Matching()
    {
        // Arrange — TryParse has a regular string param + out ReadOnlySpan<byte>
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse("hello")
            .Returns(true)
            .SetsOutData(new ReadOnlySpan<byte>([0xCA, 0xFE]));

        // Act
        var success = mock.Object.TryParse("hello", out var data);
        var length = data.Length;
        var d0 = data[0];
        var d1 = data[1];

        // Assert
        await Assert.That(success).IsTrue();
        await Assert.That(length).IsEqualTo(2);
        await Assert.That(d0).IsEqualTo((byte)0xCA);
        await Assert.That(d1).IsEqualTo((byte)0xFE);
    }

    [Test]
    public async Task Out_ReadOnlySpan_No_Setup_Stays_Default()
    {
        // Arrange — no SetsOut call
        var mock = Mock.Of<ISpanParser>();
        mock.TryParse("key").Returns(false);

        // Act
        var success = mock.Object.TryParse("key", out var data);
        var length = data.Length;

        // Assert — out param stays default (empty span)
        await Assert.That(success).IsFalse();
        await Assert.That(length).IsEqualTo(0);
    }
}
