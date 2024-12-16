namespace TUnit.Core.Interfaces;

public interface ILastTestInClassEventReceiver : IEventReceiver
{
    ValueTask OnLastTestInClass(ClassHookContext context, TestContext testContext);
}