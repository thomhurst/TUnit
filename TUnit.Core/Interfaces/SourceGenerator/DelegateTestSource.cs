namespace TUnit.Core.Interfaces.SourceGenerator;

internal sealed class DelegateTestSource : ITestSource, ITestDescriptorSource
{
    private readonly Func<string, IReadOnlyList<TestMetadata>> _getTests;
    private readonly Func<IEnumerable<TestDescriptor>> _enumerateDescriptors;

    public DelegateTestSource(
        Func<string, IReadOnlyList<TestMetadata>> getTests,
        Func<IEnumerable<TestDescriptor>> enumerateDescriptors)
    {
        _getTests = getTests;
        _enumerateDescriptors = enumerateDescriptors;
    }

    public IReadOnlyList<TestMetadata> GetTests(string testSessionId)
        => _getTests(testSessionId);

    public IEnumerable<TestDescriptor> EnumerateTestDescriptors()
        => _enumerateDescriptors();
}
