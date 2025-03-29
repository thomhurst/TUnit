namespace TUnit.Core.Interfaces.SourceGenerator;

public interface IDynamicTestSource
{
    IReadOnlyList<DynamicTest> CollectTests(string sessionId);
}