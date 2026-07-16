using TUnit.Core.Executors;
using TUnit.TestProject.Attributes;
using TUnit.TestProject.TestExecutors;

namespace TUnit.TestProject;

/// <summary>
/// Tests to verify that TestExecutorAttribute respects scope hierarchy:
/// method-level overrides class-level, which overrides assembly-level.
/// See: https://github.com/thomhurst/TUnit/issues/4351
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[TestExecutor<ClassScopeExecutor>]
public class TestExecutorScopeHierarchyTests
{
    [Test]
    [TestExecutor<MethodScopeExecutor>]
    public async Task MethodLevelExecutor_ShouldOverride_ClassLevelExecutor()
    {
        // Method-level [TestExecutor<MethodScopeExecutor>] should take precedence
        // over class-level [TestExecutor<ClassScopeExecutor>]
        var executorUsed = ScopeTrackingExecutor.GetExecutorUsed(nameof(MethodLevelExecutor_ShouldOverride_ClassLevelExecutor));

        await Assert.That(executorUsed)
            .IsNotNull()
            .And
            .IsEqualTo("Method");
    }

    [Test]
    public async Task ClassLevelExecutor_ShouldBeUsed_WhenNoMethodLevelExists()
    {
        // Without a method-level attribute, the class-level [TestExecutor<ClassScopeExecutor>]
        // should be used
        var executorUsed = ScopeTrackingExecutor.GetExecutorUsed(nameof(ClassLevelExecutor_ShouldBeUsed_WhenNoMethodLevelExists));

        await Assert.That(executorUsed)
            .IsNotNull()
            .And
            .IsEqualTo("Class");
    }
}

/// <summary>
/// Second test class to verify class-level executor is properly scoped per class.
/// This class has a different class-level executor than TestExecutorScopeHierarchyTests.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[TestExecutor<MethodScopeExecutor>] // Using MethodScopeExecutor at class level to verify isolation
public class TestExecutorScopeHierarchyTests2
{
    [Test]
    [TestExecutor<ClassScopeExecutor>] // Intentionally "reversed" to verify it works both ways
    public async Task MethodLevelExecutor_OverridesClassLevel_EvenWhenReversed()
    {
        // Method-level should still win even when we use "ClassScopeExecutor" at method level
        // and "MethodScopeExecutor" at class level - the scope hierarchy is about precedence,
        // not the executor names
        var executorUsed = ScopeTrackingExecutor.GetExecutorUsed(nameof(MethodLevelExecutor_OverridesClassLevel_EvenWhenReversed));

        await Assert.That(executorUsed)
            .IsNotNull()
            .And
            .IsEqualTo("Class"); // "Class" because ClassScopeExecutor.ScopeName == "Class"
    }

    [Test]
    public async Task ClassLevelExecutor_UsedWhenNoMethodLevel()
    {
        // Should use the class-level executor (which happens to be MethodScopeExecutor in this class)
        var executorUsed = ScopeTrackingExecutor.GetExecutorUsed(nameof(ClassLevelExecutor_UsedWhenNoMethodLevel));

        await Assert.That(executorUsed)
            .IsNotNull()
            .And
            .IsEqualTo("Method"); // "Method" because MethodScopeExecutor.ScopeName == "Method"
    }
}

/// <summary>
/// Tests using the non-generic TestExecutorAttribute(typeof(...)) overload.
/// </summary>
[EngineTest(ExpectedResult.Pass)]
[TestExecutor(typeof(ClassScopeExecutor))]
public class TestExecutorScopeHierarchyNonGenericTests
{
    [Test]
    [TestExecutor(typeof(MethodScopeExecutor))]
    public async Task NonGeneric_MethodLevelExecutor_ShouldOverride_ClassLevelExecutor()
    {
        var executorUsed = ScopeTrackingExecutor.GetExecutorUsed(nameof(NonGeneric_MethodLevelExecutor_ShouldOverride_ClassLevelExecutor));

        await Assert.That(executorUsed)
            .IsNotNull()
            .And
            .IsEqualTo("Method");
    }

    [Test]
    public async Task NonGeneric_ClassLevelExecutor_ShouldBeUsed_WhenNoMethodLevelExists()
    {
        var executorUsed = ScopeTrackingExecutor.GetExecutorUsed(nameof(NonGeneric_ClassLevelExecutor_ShouldBeUsed_WhenNoMethodLevelExists));

        await Assert.That(executorUsed)
            .IsNotNull()
            .And
            .IsEqualTo("Class");
    }
}
