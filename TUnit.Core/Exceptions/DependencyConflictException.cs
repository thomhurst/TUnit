namespace TUnit.Core.Exceptions;

public class DependencyConflictException : TUnitException
{
    internal DependencyConflictException(IEnumerable<TestDetails> testChain) : base(GetMessage(testChain.ToList()))
    {
    }

    private static string GetMessage(List<TestDetails> testChain)
    {
        return $"DependsOn Conflict: {string.Join(" > ", testChain.Select(x => $"{x.MethodMetadata.Class.Name}.{x.TestName}"))}";
    }
}
