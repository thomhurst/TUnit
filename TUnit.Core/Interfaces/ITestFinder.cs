namespace TUnit.Core.Interfaces;

public interface ITestFinder
{
    IEnumerable<TestContext> GetTests(Type classType);
    
    TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes,
        Type classType, IEnumerable<Type> classParameterTypes);
}