using TUnit.Mocks;
using TUnit.Mocks.Arguments;

namespace TUnit.Mocks.Tests;

// ─── Interface with ref struct methods ───────────────────────────────────────

public interface IBufferProcessor
{
    void Process(ReadOnlySpan<byte> data);
    int Parse(ReadOnlySpan<char> text);
    string GetName();
    void Clear();
}

public interface IMixedProcessor
{
    int Compute(int id, ReadOnlySpan<byte> data);
    void Send(string destination, ReadOnlySpan<byte> payload);
}

// ─── Tests ───────────────────────────────────────────────────────────────────

/// <summary>
/// Tests that interfaces with ref struct parameters can be mocked.
/// Ref struct params are excluded from argument matching; other methods work normally.
/// </summary>
public class RefStructTests
{
    [Test]
    public async Task Normal_Method_Returns_Configured_Value()
    {
        // Arrange
        var mock = Mock.Of<IBufferProcessor>();
        mock.GetName().Returns("processor-1");

        // Act
        IBufferProcessor processor = mock.Object;
        var name = processor.GetName();

        // Assert
        await Assert.That(name).IsEqualTo("processor-1");
    }

#if !NET9_0_OR_GREATER

    [Test]
    public async Task Void_RefStruct_Method_Callback_Fires()
    {
        // Arrange
        var wasCalled = false;
        var mock = Mock.Of<IBufferProcessor>();
        mock.Process().Callback(() => wasCalled = true);

        // Act
        IBufferProcessor processor = mock.Object;
        processor.Process(new byte[] { 1, 2, 3 });

        // Assert
        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task Void_RefStruct_Method_Verification_Works()
    {
        // Arrange
        var mock = Mock.Of<IBufferProcessor>();
        IBufferProcessor processor = mock.Object;

        // Act
        processor.Process(new byte[] { 1, 2, 3 });
        processor.Process(ReadOnlySpan<byte>.Empty);

        // Assert
        mock.Process().WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task Void_RefStruct_Method_Throws_Configured_Exception()
    {
        // Arrange
        var mock = Mock.Of<IBufferProcessor>();
        mock.Process().Throws<InvalidOperationException>();

        // Act & Assert
        IBufferProcessor processor = mock.Object;
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            processor.Process(new byte[] { 1 });
        });

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task NonVoid_RefStruct_Param_Method_Returns_Configured_Value()
    {
        // Arrange — Parse takes ReadOnlySpan<char> param but returns int
        var mock = Mock.Of<IBufferProcessor>();
        mock.Parse().Returns(42);

        // Act
        IBufferProcessor processor = mock.Object;
        var result = processor.Parse("hello".AsSpan());

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task NonVoid_RefStruct_Param_Verification()
    {
        // Arrange
        var mock = Mock.Of<IBufferProcessor>();
        mock.Parse().Returns(0);

        // Act
        IBufferProcessor processor = mock.Object;
        processor.Parse("abc".AsSpan());
        processor.Parse("xyz".AsSpan());

        // Assert
        mock.Parse().WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

#endif

    [Test]
    public async Task Void_Normal_Method_Still_Works()
    {
        // Arrange
        var wasCalled = false;
        var mock = Mock.Of<IBufferProcessor>();
        mock.Clear().Callback(() => wasCalled = true);

        // Act
        IBufferProcessor processor = mock.Object;
        processor.Clear();

        // Assert
        await Assert.That(wasCalled).IsTrue();
        mock.Clear().WasCalled(Times.Once);
    }

#if !NET9_0_OR_GREATER

    [Test]
    public async Task Mixed_Params_ArgMatching_On_NonRefStruct_Params()
    {
        // Arrange — Compute(int id, ReadOnlySpan<byte> data) returns int
        // Only 'id' participates in argument matching
        var mock = Mock.Of<IMixedProcessor>();
        mock.Compute(1).Returns(100);
        mock.Compute(2).Returns(200);

        // Act
        IMixedProcessor processor = mock.Object;
        var result1 = processor.Compute(1, new byte[] { 0xFF });
        var result2 = processor.Compute(2, ReadOnlySpan<byte>.Empty);
        var result3 = processor.Compute(99, new byte[] { 0x00 });

        // Assert — argument matching works on the int param
        await Assert.That(result1).IsEqualTo(100);
        await Assert.That(result2).IsEqualTo(200);
        await Assert.That(result3).IsEqualTo(0); // no setup for id=99, returns default
    }

    [Test]
    public async Task Mixed_Params_Verification_With_Matcher()
    {
        // Arrange
        var mock = Mock.Of<IMixedProcessor>();
        IMixedProcessor processor = mock.Object;

        // Act
        processor.Send("server-a", new byte[] { 1, 2, 3 });
        processor.Send("server-b", ReadOnlySpan<byte>.Empty);
        processor.Send("server-a", new byte[] { 4, 5, 6 });

        // Assert — verify by the string destination (non-ref-struct param)
        mock.Send("server-a").WasCalled(Times.Exactly(2));
        mock.Send("server-b").WasCalled(Times.Once);
        mock.Send(Arg.Any<string>()).WasCalled(Times.Exactly(3));
        await Assert.That(true).IsTrue();
    }

#endif

#if NET9_0_OR_GREATER

    [Test]
    public async Task RefStructArg_Any_Matches_Void_Method()
    {
        // Arrange
        var wasCalled = false;
        var mock = Mock.Of<IBufferProcessor>();
        mock.Process(RefStructArg<ReadOnlySpan<byte>>.Any).Callback(() => wasCalled = true);

        // Act
        mock.Object.Process(new byte[] { 1, 2, 3 });

        // Assert
        await Assert.That(wasCalled).IsTrue();
    }

    [Test]
    public async Task RefStructArg_Any_Matches_Return_Method()
    {
        // Arrange
        var mock = Mock.Of<IBufferProcessor>();
        mock.Parse(RefStructArg<ReadOnlySpan<char>>.Any).Returns(99);

        // Act
        var result = mock.Object.Parse("test".AsSpan());

        // Assert
        await Assert.That(result).IsEqualTo(99);
    }

    [Test]
    public async Task RefStructArg_Mixed_Params_Works()
    {
        // Arrange — Compute(int id, ReadOnlySpan<byte> data)
        var mock = Mock.Of<IMixedProcessor>();
        mock.Compute(1, RefStructArg<ReadOnlySpan<byte>>.Any).Returns(100);
        mock.Compute(2, RefStructArg<ReadOnlySpan<byte>>.Any).Returns(200);

        // Act
        var result1 = mock.Object.Compute(1, new byte[] { 0xFF });
        var result2 = mock.Object.Compute(2, ReadOnlySpan<byte>.Empty);

        // Assert
        await Assert.That(result1).IsEqualTo(100);
        await Assert.That(result2).IsEqualTo(200);
    }

    [Test]
    public async Task RefStructArg_Verification_With_Any()
    {
        // Arrange
        var mock = Mock.Of<IBufferProcessor>();
        mock.Object.Process(new byte[] { 1, 2, 3 });
        mock.Object.Process(ReadOnlySpan<byte>.Empty);

        // Assert
        mock.Process(RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RefStructArg_Mixed_Verification()
    {
        // Arrange — Send(string destination, ReadOnlySpan<byte> payload)
        var mock = Mock.Of<IMixedProcessor>();
        mock.Object.Send("server-a", new byte[] { 1, 2, 3 });
        mock.Object.Send("server-b", ReadOnlySpan<byte>.Empty);

        // Assert — verify with both Arg<string> and RefStructArg<ReadOnlySpan<byte>>
        mock.Send("server-a", RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Once);
        mock.Send(Arg.Any<string>(), RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RefStructArg_Void_Method_Throws_Configured_Exception()
    {
        // Arrange
        var mock = Mock.Of<IBufferProcessor>();
        mock.Process(RefStructArg<ReadOnlySpan<byte>>.Any).Throws<InvalidOperationException>();

        // Act & Assert
        IBufferProcessor processor = mock.Object;
        var ex = Assert.Throws<InvalidOperationException>(() =>
        {
            processor.Process(new byte[] { 1 });
        });

        await Assert.That(ex).IsNotNull();
    }

    [Test]
    public async Task RefStructArg_NonVoid_Method_Returns_Configured_Value()
    {
        // Arrange — Parse takes ReadOnlySpan<char> param but returns int
        var mock = Mock.Of<IBufferProcessor>();
        mock.Parse(RefStructArg<ReadOnlySpan<char>>.Any).Returns(42);

        // Act
        IBufferProcessor processor = mock.Object;
        var result = processor.Parse("hello".AsSpan());

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task RefStructArg_NonVoid_Method_Verification()
    {
        // Arrange
        var mock = Mock.Of<IBufferProcessor>();
        mock.Parse(RefStructArg<ReadOnlySpan<char>>.Any).Returns(0);

        // Act
        IBufferProcessor processor = mock.Object;
        processor.Parse("abc".AsSpan());
        processor.Parse("xyz".AsSpan());

        // Assert
        mock.Parse(RefStructArg<ReadOnlySpan<char>>.Any).WasCalled(Times.Exactly(2));
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task RefStructArg_Mixed_Params_ArgMatching_On_NonRefStruct_Params()
    {
        // Arrange — Compute(int id, ReadOnlySpan<byte> data) returns int
        // Both params participate in matching on net9.0+ via RefStructArg.Any
        var mock = Mock.Of<IMixedProcessor>();
        mock.Compute(1, RefStructArg<ReadOnlySpan<byte>>.Any).Returns(100);
        mock.Compute(2, RefStructArg<ReadOnlySpan<byte>>.Any).Returns(200);

        // Act
        IMixedProcessor processor = mock.Object;
        var result1 = processor.Compute(1, new byte[] { 0xFF });
        var result2 = processor.Compute(2, ReadOnlySpan<byte>.Empty);
        var result3 = processor.Compute(99, new byte[] { 0x00 });

        // Assert — argument matching works on the int param
        await Assert.That(result1).IsEqualTo(100);
        await Assert.That(result2).IsEqualTo(200);
        await Assert.That(result3).IsEqualTo(0); // no setup for id=99, returns default
    }

    [Test]
    public async Task RefStructArg_Mixed_Params_Verification_With_Matcher()
    {
        // Arrange
        var mock = Mock.Of<IMixedProcessor>();
        IMixedProcessor processor = mock.Object;

        // Act
        processor.Send("server-a", new byte[] { 1, 2, 3 });
        processor.Send("server-b", ReadOnlySpan<byte>.Empty);
        processor.Send("server-a", new byte[] { 4, 5, 6 });

        // Assert — verify by the string destination (non-ref-struct param) with RefStructArg.Any
        mock.Send("server-a", RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Exactly(2));
        mock.Send("server-b", RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Once);
        mock.Send(Arg.Any<string>(), RefStructArg<ReadOnlySpan<byte>>.Any).WasCalled(Times.Exactly(3));
        await Assert.That(true).IsTrue();
    }

#endif
}
