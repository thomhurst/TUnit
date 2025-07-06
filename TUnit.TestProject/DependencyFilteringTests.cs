namespace TUnit.TestProject;

public class DependencyFilteringTests
{
    private static readonly List<string> ExecutedTests = new();

    [Test]
    public async Task BaseTest()
    {
        ExecutedTests.Add(nameof(BaseTest));

        await Assert.That(ExecutedTests).Contains(nameof(BaseTest));
    }

    [Test]
    [DependsOn(nameof(BaseTest))]
    public async Task DependentTest()
    {
        ExecutedTests.Add(nameof(DependentTest));

        await Assert.That(ExecutedTests).Contains(nameof(BaseTest));
        await Assert.That(ExecutedTests).Contains(nameof(DependentTest));
    }

    [Test]
    [DependsOn(nameof(DependentTest))]
    public async Task DeepDependentTest()
    {
        ExecutedTests.Add(nameof(DeepDependentTest));

        await Assert.That(ExecutedTests).Contains(nameof(BaseTest));
        await Assert.That(ExecutedTests).Contains(nameof(DependentTest));
        await Assert.That(ExecutedTests).Contains(nameof(DeepDependentTest));

        // Verify that TestContext.Dependencies is populated
        var context = TestContext.Current!;
        await Assert.That(context.Dependencies).HasCount().EqualTo(1);
        await Assert.That(context.Dependencies[0].TestName).IsEqualTo("DependentTest");
    }

    [Test]
    public async Task IndependentTest()
    {
        ExecutedTests.Add(nameof(IndependentTest));

        await Assert.That(ExecutedTests).Contains(nameof(IndependentTest));

        // Verify that TestContext.Dependencies is empty for independent tests
        var context = TestContext.Current!;
        await Assert.That(context.Dependencies).IsEmpty();
    }

    [Test]
    [DependsOn(typeof(DependencyFilteringTests2), nameof(DependencyFilteringTests2.CrossClassDependency))]
    public async Task TestWithCrossClassDependency()
    {
        ExecutedTests.Add(nameof(TestWithCrossClassDependency));

        await Assert.That(DependencyFilteringTests2.ExecutedTests).Contains(nameof(DependencyFilteringTests2.CrossClassDependency));
        await Assert.That(ExecutedTests).Contains(nameof(TestWithCrossClassDependency));

        // Verify that TestContext.Dependencies is populated with cross-class dependency
        var context = TestContext.Current!;
        await Assert.That(context.Dependencies).HasCount().EqualTo(1);
        await Assert.That(context.Dependencies[0].TestName).IsEqualTo("CrossClassDependency");
        await Assert.That(context.Dependencies[0].ClassType.Name).IsEqualTo("DependencyFilteringTests2");
    }
}

public class DependencyFilteringTests2
{
    public static readonly List<string> ExecutedTests = new();

    [Test]
    public async Task CrossClassDependency()
    {
        ExecutedTests.Add(nameof(CrossClassDependency));

        await Assert.That(ExecutedTests).Contains(nameof(CrossClassDependency));
    }
}
