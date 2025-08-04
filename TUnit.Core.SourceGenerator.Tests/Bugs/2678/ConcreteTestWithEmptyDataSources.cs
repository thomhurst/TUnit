namespace TUnit.Core.SourceGenerator.Tests.Bugs._2678;

[InheritsTests]
public sealed class ConcreteTestWithEmptyDataSources() : AbstractTestWithEmptyDataSources(new object(), [])
{
    // This should trigger the issue when there are empty data sources in the base class
    // With the fix, this should generate warnings but not fail the build
}