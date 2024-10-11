namespace TUnit.Core.Interfaces;

public interface ILastTestInAssemblyEvent
{
    ValueTask IfLastTestInAssembly(AssemblyHookContext context, TestContext testContext);
}