namespace TUnit.Core.Interfaces.SourceGenerator;

public interface ITestSource
{
    IEnumerable<TestMetadata> GetTests();
}
