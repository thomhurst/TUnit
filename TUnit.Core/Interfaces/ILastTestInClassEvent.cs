namespace TUnit.Core.Interfaces;

public interface ILastTestInClassEvent
{
    ValueTask IfLastTestInClass(ClassHookContext context, TestContext testContext);
}