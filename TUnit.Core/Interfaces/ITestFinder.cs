namespace TUnit.Core.Interfaces;

public interface ITestFinder
{
    TestContext[] GetTestsByNameAndParameters(string testName, IEnumerable<Type> methodParameterTypes,
        Type classType, IEnumerable<Type> classParameterTypes);
}