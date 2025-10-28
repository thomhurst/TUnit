using TUnit.Core;
using TUnit.Core.Extensions;

namespace TUnit.TestProject;

public class TestVariantTests
{
    [Test]
    public async Task CreateTestVariant_ShouldCreateVariantWithDifferentArguments()
    {
        var context = TestContext.Current;

        if (context == null)
        {
            throw new InvalidOperationException("TestContext.Current is null");
        }

        await context.CreateTestVariant(
            arguments: new object?[] { 42 },
            properties: new Dictionary<string, object?>
            {
                { "ShrinkAttempt", 1 }
            }
        );
    }

    [Test]
    [Arguments(10)]
    public async Task VariantTarget_WithArguments(int value)
    {
        var context = TestContext.Current;

        if (context == null)
        {
            throw new InvalidOperationException("TestContext.Current is null");
        }

        if (value < 0)
        {
            throw new InvalidOperationException($"Expected non-negative value but got {value}");
        }

        if (context.ObjectBag.ContainsKey("ShrinkAttempt"))
        {
            var shrinkAttempt = context.ObjectBag["ShrinkAttempt"];
            context.WriteLine($"Shrink attempt {shrinkAttempt} with value {value}");

            if (context.Relationship != TUnit.Core.Enums.TestRelationship.ShrinkAttempt)
            {
                throw new InvalidOperationException($"Expected ShrinkAttempt relationship but got {context.Relationship}");
            }

            if (context.ParentTestId == null)
            {
                throw new InvalidOperationException("Expected ParentTestId to be set for shrink attempt");
            }
        }
    }
}
