using TUnit.Core.Enums;
using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class OverrideResultsTests
{
    [Test, OverridePass]
    public void OverrideResult_Throws_When_TestResult_Is_Null()
    {
        throw new InvalidOperationException();
    }

    [After(Class)]
    public static async Task AfterClass(ClassHookContext classHookContext)
    {
        await Assert.That(classHookContext.Tests)
            .HasSingleItem()
            .And
            .ContainsOnly(t => t.Result?.Status == Status.Passed);
    }

    public class OverridePassAttribute : Attribute, ITestEndEventReceiver
    {
        public ValueTask OnTestEnd(TestContext afterTestContext)
        {
            afterTestContext.OverrideResult(Status.Passed, "Because I said so");
            return default;
        }

        public int Order => 0;
    }
}
