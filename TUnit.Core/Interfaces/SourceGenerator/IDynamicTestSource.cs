namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IDynamicTestSource
{
    IReadOnlyList<DynamicTest> CollectDynamicTests(string sessionId);
}