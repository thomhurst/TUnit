namespace TUnit.Core.Interfaces;

public interface ILastTestInAssemblyEventReceiver : IEventReceiver
{
    ValueTask OnLastTestInAssembly(AssemblyHookContext context, TestContext testContext);
}