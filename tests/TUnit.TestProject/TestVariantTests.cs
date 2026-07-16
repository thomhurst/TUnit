using TUnit.Core;
using TUnit.Core.Extensions;

namespace TUnit.TestProject;

public class TestVariantTests
{
    [Test]
    [Arguments(10)]
    public async Task CreateTestVariant_ShouldCreateVariantWithDifferentArguments(int value)
    {
        var context = TestContext.Current!;

        // Only the original test creates a variant; variants skip to avoid infinite recursion
        if (!context.Dependencies.IsVariant())
        {
            var variantInfo = await context.CreateTestVariant(
                methodArguments: [42],
                properties: new Dictionary<string, object?>
                {
                    { "AttemptNumber", 1 }
                },
                relationship: TUnit.Core.Enums.TestRelationship.Derived,
                displayName: "Shrink Attempt"
            );

            if (string.IsNullOrEmpty(variantInfo.TestId))
            {
                throw new InvalidOperationException("Expected TestVariantInfo.TestId to be set");
            }

            if (variantInfo.DisplayName != "Shrink Attempt")
            {
                throw new InvalidOperationException($"Expected DisplayName 'Shrink Attempt' but got '{variantInfo.DisplayName}'");
            }
        }

        if (value < 0)
        {
            throw new InvalidOperationException($"Expected non-negative value but got {value}");
        }

        if (context.Dependencies.IsVariant())
        {
            if (context.StateBag.Items.TryGetValue("AttemptNumber", out var attemptNumber))
            {
                context.Output.StandardOutput.WriteLine($"Shrink attempt {attemptNumber} with value {value}");
            }

            if (context.Dependencies.Relationship != TUnit.Core.Enums.TestRelationship.Derived)
            {
                throw new InvalidOperationException($"Expected Derived relationship but got {context.Dependencies.Relationship}");
            }

            if (context.Dependencies.ParentTestId == null)
            {
                throw new InvalidOperationException("Expected ParentTestId to be set for shrink attempt");
            }
        }
    }

    // Regression tests for #5093 - CreateTestVariant must handle all test return types.
    // Each return type produces a different expression tree shape:
    //   Task        → direct MethodCallExpression
    //   ValueTask   → MethodCallExpression wrapped in AsTask() call
    //   Task<T>     → UnaryExpression (Convert) wrapping MethodCallExpression
    //   void        → BlockExpression (only reachable via AddDynamicTest, not CreateTestVariant)
    //   ValueTask<T>→ source generator doesn't support this return type yet
    // void and ValueTask<T> are covered by unit tests in ExpressionHelperTests.

    [Test]
    public async Task CreateTestVariant_FromTaskMethod()
    {
        if (!TestContext.Current!.Dependencies.IsVariant())
        {
            await TestContext.Current!.CreateTestVariant(
                displayName: "VariantFromTaskMethod",
                relationship: TUnit.Core.Enums.TestRelationship.Generated
            );
        }
    }

    [Test]
    public async ValueTask CreateTestVariant_FromValueTaskMethod()
    {
        if (!TestContext.Current!.Dependencies.IsVariant())
        {
            await TestContext.Current!.CreateTestVariant(
                displayName: "VariantFromValueTaskMethod",
                relationship: TUnit.Core.Enums.TestRelationship.Generated
            );
        }
    }

    [Test]
    public async Task<int> CreateTestVariant_FromGenericTaskMethod()
    {
        if (!TestContext.Current!.Dependencies.IsVariant())
        {
            await TestContext.Current!.CreateTestVariant(
                displayName: "VariantFromGenericTaskMethod",
                relationship: TUnit.Core.Enums.TestRelationship.Generated
            );
        }

        return 42;
    }

}

[Arguments("original")]
public class TestVariantWithClassArgsTests(string label)
{
    [Test]
    public async Task CreateTestVariant_ShouldPassClassArguments()
    {
        var context = TestContext.Current!;

        if (!context.Dependencies.IsVariant())
        {
            await context.CreateTestVariant(
                classArguments: ["variant-label"],
                displayName: "VariantWithClassArgs"
            );
        }

        // Both original and variant should have a non-null label
        if (string.IsNullOrEmpty(label))
        {
            throw new InvalidOperationException("Expected label to be set");
        }

        if (context.Dependencies.IsVariant() && label != "variant-label")
        {
            throw new InvalidOperationException($"Expected variant label 'variant-label' but got '{label}'");
        }
    }
}
