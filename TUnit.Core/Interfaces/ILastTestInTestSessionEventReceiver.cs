namespace TUnit.Core.Interfaces;

public interface ILastTestInTestSessionEventReceiver : IEventReceiver
{
    ValueTask OnLastTestInTestSession(TestSessionContext current, TestContext testContext);
}