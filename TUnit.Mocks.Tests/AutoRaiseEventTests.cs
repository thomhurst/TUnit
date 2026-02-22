namespace TUnit.Mocks.Tests;

public interface IProcessService
{
    event EventHandler<string> StatusChanged;
    bool Process(int id);
    void Execute(string command);
}

public class AutoRaiseEventTests
{
    [Test]
    public async Task Raises_Event_When_Method_Returns_Value()
    {
        var mock = Mock.Of<IProcessService>();
        string? receivedStatus = null;

        mock.Object.StatusChanged += (sender, status) => receivedStatus = status;

        mock.Setup.Process(Arg.Any<int>())
            .Returns(true)
            .Raises(nameof(IProcessService.StatusChanged), "completed");

        mock.Object.Process(42);

        await Assert.That(receivedStatus).IsEqualTo("completed");
    }

    [Test]
    public async Task Raises_Event_When_Void_Method_Called()
    {
        var mock = Mock.Of<IProcessService>();
        string? receivedStatus = null;

        mock.Object.StatusChanged += (sender, status) => receivedStatus = status;

        mock.Setup.Execute(Arg.Any<string>())
            .Raises(nameof(IProcessService.StatusChanged), "executed");

        mock.Object.Execute("run");

        await Assert.That(receivedStatus).IsEqualTo("executed");
    }

    [Test]
    public async Task Multiple_Raises_Fire_In_Order()
    {
        var mock = Mock.Of<IProcessService>();
        var receivedStatuses = new List<string>();

        mock.Object.StatusChanged += (sender, status) => receivedStatuses.Add(status);

        mock.Setup.Process(Arg.Any<int>())
            .Returns(true)
            .Raises(nameof(IProcessService.StatusChanged), "first")
            .Raises(nameof(IProcessService.StatusChanged), "second");

        mock.Object.Process(1);

        await Assert.That(receivedStatuses).HasCount().EqualTo(2);
        await Assert.That(receivedStatuses[0]).IsEqualTo("first");
        await Assert.That(receivedStatuses[1]).IsEqualTo("second");
    }

    [Test]
    public async Task Raises_With_No_Subscribers_Does_Not_Throw()
    {
        var mock = Mock.Of<IProcessService>();

        mock.Setup.Process(Arg.Any<int>())
            .Returns(true)
            .Raises(nameof(IProcessService.StatusChanged), "ignored");

        // No subscriber — should not throw
        var result = mock.Object.Process(1);

        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task Raises_On_Each_Call()
    {
        var mock = Mock.Of<IProcessService>();
        var callCount = 0;

        mock.Object.StatusChanged += (sender, status) => callCount++;

        mock.Setup.Process(Arg.Any<int>())
            .Returns(true)
            .Raises(nameof(IProcessService.StatusChanged), "ping");

        mock.Object.Process(1);
        mock.Object.Process(2);
        mock.Object.Process(3);

        await Assert.That(callCount).IsEqualTo(3);
    }
}
