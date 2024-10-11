namespace TUnit.Core.Interfaces;

public interface ILastTestInTestSessionEvent
{
    ValueTask IfLastTestInTestSession(TestSessionContext current, TestContext testContext);
}