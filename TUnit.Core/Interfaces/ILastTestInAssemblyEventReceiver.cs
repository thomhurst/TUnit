namespace TUnit.Core.Interfaces;

public interface ILastTestInAssemblyEventReceiver : IEventReceiver
{
    ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext);
}