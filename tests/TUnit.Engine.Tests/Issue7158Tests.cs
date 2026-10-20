using Shouldly;
using TUnit.Engine.Tests.Enums;

namespace TUnit.Engine.Tests;

/// <summary>
/// A generic method with several [GenerateGenericTest] types and one typed ClassDataSource&lt;T&gt; per
/// type runs each instantiation only with the data source of its type, in both modes. Source-gen mode
/// used to pair every instantiation with every data source, failing the mismatched pairs with an
/// InvalidCastException (#7158).
/// </summary>
public class Issue7158Tests(TestMode testMode) : InvokableTestBase(testMode)
{
    [Test]
    public async Task Each_Instantiation_Runs_Only_Its_Typed_Class_Data_Source()
    {
        await RunTestsWithFilter(
            "/*/TUnit.TestProject.Bugs._7158/GenerateGenericTestTypedClassDataSourceTests/*",
            [
                result => result.ResultSummary.Counters.Total.ShouldBe(2),
                result => result.ResultSummary.Counters.Passed.ShouldBe(2),
                result => result.ResultSummary.Counters.Failed.ShouldBe(0)
            ]);
    }
}
