namespace TUnit.Core.Interfaces;

public interface ILastTestInClassEventReceiver : IEventReceiver
{
    ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext);
}