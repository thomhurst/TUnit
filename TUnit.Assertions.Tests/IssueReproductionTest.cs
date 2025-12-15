using System.Numerics;

namespace TUnit.Assertions.Tests;

/// <summary>
/// Reproduces the exact test case from the GitHub issue to verify the fix.
/// This test should now pass without requiring an explicit comparer.
/// </summary>
public class IssueReproductionTest
{
    /// <summary>
    /// This is the exact test case from the GitHub issue.
    /// Previously this would fail with "Parameter count mismatch" error.
    /// Now it should pass because Vector2 implements IEquatable&lt;Vector2&gt;.
    /// </summary>
    [Test]
    public async Task Test1_FromGitHubIssue()
    {
        var array = new Vector2[]
        {
            new Vector2(1, 2),
            new Vector2(3, 4),
            new Vector2(5, 6),
        };
        var array2 = new List<Vector2>(array);
        
        await Assert.That(array).IsEquivalentTo(array2);
    }

    /// <summary>
    /// Verify the workaround mentioned in the issue still works.
    /// Users can still explicitly pass EqualityComparer if needed.
    /// </summary>
    [Test]
    public async Task Test2_WithExplicitComparer_StillWorks()
    {
        var array = new Vector2[]
        {
            new Vector2(1, 2),
            new Vector2(3, 4),
            new Vector2(5, 6),
        };
        var array2 = new List<Vector2>(array);
        
        await Assert.That(array).IsEquivalentTo(array2, EqualityComparer<Vector2>.Default);
    }
}
