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
        mock.GetValue(Any()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");

        mock.GetValue("key1").WasCalled(Times.Once);
        Mock.VerifyNoOtherCalls(mock);

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Fails_When_Unverified_Calls_Exist()
    {
        var mock = Mock.Of<IService>();
        mock.GetValue(Any()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");
        svc.Process(42);

        // Only verify GetValue, not Process
        mock.GetValue("key1").WasCalled(Times.Once);

        var ex = Assert.Throws<MockVerificationException>(() => Mock.VerifyNoOtherCalls(mock));
        await Assert.That(ex.Message).Contains("Process(42)");
    }

    [Test]
    public async Task VerifyNoOtherCalls_Passes_When_No_Calls_Made()
    {
        var mock = Mock.Of<IService>();
        Mock.VerifyNoOtherCalls(mock);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Works_After_Reset()
    {
        var mock = Mock.Of<IService>();
        mock.GetValue(Any()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("key1");

        Mock.Reset(mock);
        Mock.VerifyNoOtherCalls(mock); // should pass — history cleared

        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyNoOtherCalls_Multiple_Unverified_Shows_All()
    {
        var mock = Mock.Of<IService>();
        mock.GetValue(Any()).Returns("value");

        var svc = mock.Object;
        svc.GetValue("a");
        svc.Process(1);
        svc.Reset();

        // Verify none
        var ex = Assert.Throws<MockVerificationException>(() => Mock.VerifyNoOtherCalls(mock));
        await Assert.That(ex.Message).Contains("GetValue(a)");
        await Assert.That(ex.Message).Contains("Process(1)");
    }
}
