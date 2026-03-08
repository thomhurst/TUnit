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
        mock.GetValue(Any()).Returns("value");
        mock.Process(Any());

        var svc = mock.Object;
        svc.GetValue("key");
        svc.Process(1);

        Mock.VerifyAll(mock);
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyAll_Fails_When_Setup_Not_Invoked()
    {
        var mock = Mock.Of<IService>();
        mock.GetValue(Any()).Returns("value");
        mock.Process(Any());

        var svc = mock.Object;
        svc.GetValue("key"); // Only call GetValue, not Process

        var ex = Assert.Throws<MockVerificationException>(() => Mock.VerifyAll(mock));
        await Assert.That(ex.Message).Contains("Process");
    }

    [Test]
    public async Task VerifyAll_Fails_When_No_Setups_Called()
    {
        var mock = Mock.Of<IService>();
        mock.GetValue(Any()).Returns("value");

        var ex = Assert.Throws<MockVerificationException>(() => Mock.VerifyAll(mock));
        await Assert.That(ex.Message).Contains("GetValue");
    }

    [Test]
    public async Task VerifyAll_Passes_When_No_Setups_Registered()
    {
        var mock = Mock.Of<IService>();
        Mock.VerifyAll(mock); // No setups = nothing to verify
        await Assert.That(true).IsTrue();
    }

    [Test]
    public async Task VerifyAll_Multiple_Uninvoked_Shows_All()
    {
        var mock = Mock.Of<IService>();
        mock.GetValue("a").Returns("val");
        mock.Process(42);

        // Don't call anything
        var ex = Assert.Throws<MockVerificationException>(() => Mock.VerifyAll(mock));
        await Assert.That(ex.Message).Contains("GetValue");
        await Assert.That(ex.Message).Contains("Process");
    }
}
