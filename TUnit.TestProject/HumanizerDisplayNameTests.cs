using Humanizer;
using TUnit.Core.Extensions;

namespace TUnit.TestProject;

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
        protected override string FormatDisplayName(TestContext testContext)
        {
            return testContext.TestDetails.TestName.Humanize();
        }
    }
}