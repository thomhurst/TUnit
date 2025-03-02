namespace TUnit.Core.Interfaces;

public interface IFirstTestInClassEventReceiver : IEventReceiver
{
    ValueTask OnFirstTestInClass(ClassHookContext context, TestContext testContext);
}