namespace TUnit.TestAdapter.Constants;

internal static class TestAdapterConstants
{
    internal const string ExecutorUriString = "executor://tunit/TestRunner/net";
    internal static readonly Uri ExecutorUri = new(ExecutorUriString);

    public const string FullyQualifiedName = "TUnit.FullyQualifiedName";
    public const string Name = "TUnit.Name";
    public const string TestCategory = "TUnit.TestCategory";
    
    public static class Filters
    {
        public static readonly IReadOnlyList<string> KnownFilters = [TestName, TestClasses, Categories];
        public const string TestName = "TestName";
        public const string TestClasses = "TestClass";
        public const string Categories = "Category";
    }
}