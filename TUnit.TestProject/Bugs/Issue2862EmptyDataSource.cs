using TUnit.Core;
using TUnit.Assertions;

namespace TUnit.TestProject.Bugs;

/// <summary>
/// Test case to reproduce issue #2862: "Test instance is null for test after instance creation" 
/// for test class with empty InstanceMethodDataSource
/// 
/// This test verifies that empty InstanceMethodDataSource collections are handled correctly
/// and don't cause "test instance is null" errors.
/// </summary>
[InheritsTests]
public sealed class Issue2862BugReproduction : Issue2862AbstractBase
{
    public Issue2862BugReproduction() : base([1, 2, 3, 4, 5, 6, /*7*/ 8, 9])
    {
    }
}

public abstract class Issue2862AbstractBase
{
    public readonly IEnumerable<int> Seven;
    public readonly IEnumerable<int> Odd;
    public readonly IEnumerable<int> Even;

    protected Issue2862AbstractBase(IEnumerable<int> data)
    {
        Seven = data.Where(d => d == 7);  // This will be empty since 7 is commented out
        Odd = data.Where(d => d % 2 == 1);
        Even = data.Where(d => d % 2 == 0);
    }

    [Test]
    [InstanceMethodDataSource(nameof(Even))]
    public async Task EvenIsEven(int d) => await Assert.That(d).IsEven();

    [Test]
    [InstanceMethodDataSource(nameof(Seven))] // This is empty and should NOT fail with "test instance is null"
    public async Task SevenIsSeven(int d) => await Assert.That(d).IsEqualTo(7);
    
    [Test]
    [InstanceMethodDataSource(nameof(Odd))]
    public async Task OddIsOdd(int d) => await Assert.That(d).IsOdd();
}