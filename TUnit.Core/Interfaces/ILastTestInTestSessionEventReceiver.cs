namespace TUnit.Core.Interfaces;

public interface ILastTestInTestSessionEventReceiver : IEventReceiver
{
    ValueTask IfLastTestInTestSession(TestSessionContext current, TestContext testContext);
}