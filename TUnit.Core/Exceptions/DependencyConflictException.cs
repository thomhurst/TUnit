namespace TUnit.Core.Exceptions;

public class DependencyConflictException : TUnitException
{
    internal DependencyConflictException(TestDetails test1, IEnumerable<TestDetails> testChain) : base(GetMessage(test1, testChain.ToList()))
    {
    }

    private static string GetMessage(TestDetails test1, List<TestDetails> testChain)
    {
        var indexOfConflict = testChain.FindIndex(x => x.IsSameTest(test1));
        return $"DependsOn Conflict: {test1.TestName} > {string.Join(" > ", testChain.Take(indexOfConflict).Select(x => x.TestName))} > {test1.TestName}";
    }
}