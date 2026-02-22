using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

public class VerifyNoOtherCallsTests
{
    public interface IService
    {
        string GetValue(string key);
        void Process(int id);
        void Reset();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Passes_When_All_Calls_Verified()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");

        mock.Verify!.GetValue("key1").WasCalled(Times.Once);
        mock.VerifyNoOtherCalls();

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Fails_When_Unverified_Calls_Exist()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");
        svc.Process(42);

        // Only verify GetValue, not Process
        mock.Verify!.GetValue("key1").WasCalled(Times.Once);

        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyNoOtherCalls());
        await Assert.That(ex.Message).Contains("Process(42)");
    }

    [Test]
    public async Task VerifyNoOtherCalls_Passes_When_No_Calls_Made()
    {
        var mock = Mock.Of<IService>();
        mock.VerifyNoOtherCalls();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Works_After_Reset()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");

        mock.Reset();
        mock.VerifyNoOtherCalls(); // should pass — history cleared

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Multiple_Unverified_Shows_All()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("a");
        svc.Process(1);
        svc.Reset();

        // Verify none
        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyNoOtherCalls());
        await Assert.That(ex.Message).Contains("GetValue(a)");
        await Assert.That(ex.Message).Contains("Process(1)");
    }
}
