using TUnit.Mocks.Exceptions;

namespace TUnit.Mocks.Tests;

public class VerifyAllTests
{
    public interface IService
    {
        string GetValue(string key);
        void Process(int id);
    }

    [Test]
    public async Task VerifyAll_Passes_When_All_Setups_Invoked()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");
        mock.Setup.Process(Arg.Any<int>());

        var svc = mock.Object;
        svc.GetValue("key");
        svc.Process(1);

        mock.VerifyAll();
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyAll_Fails_When_Setup_Not_Invoked()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");
        mock.Setup.Process(Arg.Any<int>());

        var svc = mock.Object;
        svc.GetValue("key"); // Only call GetValue, not Process

        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyAll());
        await Assert.That(ex.Message).Contains("Process");
    }

    [Test]
    public async Task VerifyAll_Fails_When_No_Setups_Called()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue(Arg.Any<string>()).Returns("value");

        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyAll());
        await Assert.That(ex.Message).Contains("GetValue");
    }

    [Test]
    public async Task VerifyAll_Passes_When_No_Setups_Registered()
    {
        var mock = Mock.Of<IService>();
        mock.VerifyAll(); // No setups = nothing to verify
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyAll_Multiple_Uninvoked_Shows_All()
    {
        var mock = Mock.Of<IService>();
        mock.Setup.GetValue("a").Returns("val");
        mock.Setup.Process(42);

        // Don't call anything
        var ex = Assert.Throws<MockVerificationException>(() => mock.VerifyAll());
        await Assert.That(ex.Message).Contains("GetValue");
        await Assert.That(ex.Message).Contains("Process");
    }
}
