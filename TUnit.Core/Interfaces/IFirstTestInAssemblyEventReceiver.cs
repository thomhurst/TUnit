namespace TUnit.Core.Interfaces;

public interface IFirstTestInAssemblyEventReceiver : IEventReceiver
{
    ValueTask OnFirstTestInAssembly(AssemblyHookContext context, TestContext testContext);
}