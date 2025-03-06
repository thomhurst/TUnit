namespace TUnit.Core.Interfaces;

public interface IFirstTestInTestSessionEventReceiver : IEventReceiver
{
    ValueTask OnFirstTestInTestSession(TestSessionContext current, TestContext testContext);
}