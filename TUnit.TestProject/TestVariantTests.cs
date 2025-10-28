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
                { "AttemptNumber", 1 }
            },
            relationship: TUnit.Core.Enums.TestRelationship.Derived,
            displayName: "Shrink Attempt"
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

        if (context.ObjectBag.ContainsKey("AttemptNumber"))
        {
            var attemptNumber = context.ObjectBag["AttemptNumber"];
            context.WriteLine($"Shrink attempt {attemptNumber} with value {value}");

            if (context.Relationship != TUnit.Core.Enums.TestRelationship.Derived)
            {
                throw new InvalidOperationException($"Expected Derived relationship but got {context.Relationship}");
            }

            if (context.ParentTestId == null)
            {
                throw new InvalidOperationException("Expected ParentTestId to be set for shrink attempt");
            }
        }
    }
}
