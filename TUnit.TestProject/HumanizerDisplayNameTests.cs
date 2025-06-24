using Humanizer;
using TUnit.Core.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[HumanizerDisplayName]
public class HumanizerDisplayNameTests
{
    [Test]
    public void This_test_name_is_formatted_nicely()
    {
        // Dummy method
        Console.WriteLine(TestContext.Current!.GetTestDisplayName());
    }

    public class HumanizerDisplayNameAttribute : DisplayNameFormatterAttribute
    {
        protected override string FormatDisplayName(DiscoveredTestContext context)
        {
            return context.TestDetails.TestName.Humanize();
        }
    }
}