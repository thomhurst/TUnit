using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._3990;

/// <summary>
/// Tests for GitHub Issue #3990: Support CombinedDataSources on Class level
/// </summary>

#region Test 1: Class-level CombinedDataSources with static Arguments

[EngineTest(ExpectedResult.Pass)]
[CombinedDataSources]
public class ClassLevelCombinedDataSources_WithStaticArguments(
    [Arguments(1, 2, 3)] int x,
    [Arguments("a", "b")] string y)
{
    [Test]
    public async Task Test_ShouldReceiveConstructorParameters()
    {
        // Should create 3 x 2 = 6 test cases
        await Assert.That(x).IsIn([1, 2, 3]);
        await Assert.That(y).IsIn(["a", "b"]);
    }
}

#endregion

#region Test 2: Class-level CombinedDataSources with static MethodDataSource

[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
public class ClassLevelDataProviders
{
    public static IEnumerable<int> GetNumbers()
    {
        yield return 10;
        yield return 20;
    }

    public static IEnumerable<string> GetStrings()
    {
        yield return "Hello";
        yield return "World";
    }

    public static IEnumerable<string> GetColors()
    {
        yield return "Red";
        yield return "Blue";
        yield return "Green";
    }
}

[EngineTest(ExpectedResult.Pass)]
[CombinedDataSources]
[DynamicCodeOnly]
public class ClassLevelCombinedDataSources_WithStaticMethodDataSource(
    [MethodDataSource<ClassLevelDataProviders>(nameof(ClassLevelDataProviders.GetNumbers))] int number,
    [MethodDataSource<ClassLevelDataProviders>(nameof(ClassLevelDataProviders.GetStrings))] string text)
{
    [Test]
    public async Task Test_ShouldReceiveDataFromStaticMethods()
    {
        // Should create 2 x 2 = 4 test cases
        await Assert.That(number).IsIn([10, 20]);
        await Assert.That(text).IsIn(["Hello", "World"]);
    }
}

#endregion

#region Test 3: Class-level CombinedDataSources mixed with method-level data sources

[EngineTest(ExpectedResult.Pass)]
[CombinedDataSources]
public class ClassLevelCombinedDataSources_MixedWithMethodLevel(
    [Arguments(1, 2)] int classArg,
    [Arguments("A", "B")] string classText)
{
    [Test]
    [CombinedDataSources]
    public async Task Test_ShouldCombineClassAndMethodData(
        [Arguments(100, 200)] int methodArg,
        [Arguments("X", "Y")] string methodText)
    {
        // Class: 2 x 2 = 4 combinations
        // Method: 2 x 2 = 4 combinations
        // Total: 4 x 4 = 16 test cases
        await Assert.That(classArg).IsIn([1, 2]);
        await Assert.That(classText).IsIn(["A", "B"]);
        await Assert.That(methodArg).IsIn([100, 200]);
        await Assert.That(methodText).IsIn(["X", "Y"]);
    }
}

#endregion

#region Test 4: Class-level CombinedDataSources with mixed data source types

[EngineTest(ExpectedResult.Pass)]
[CombinedDataSources]
[DynamicCodeOnly]
public class ClassLevelCombinedDataSources_MixedDataSourceTypes(
    [Arguments(1, 2)] int number,
    [MethodDataSource<ClassLevelDataProviders>(nameof(ClassLevelDataProviders.GetColors))] string color)
{
    [Test]
    public async Task Test_ShouldMixArgumentsAndMethodDataSource()
    {
        // Should create 2 x 3 = 6 test cases
        await Assert.That(number).IsIn([1, 2]);
        await Assert.That(color).IsIn(["Red", "Blue", "Green"]);
    }
}

#endregion

#region Test 5: Class-level CombinedDataSources with three constructor parameters

[EngineTest(ExpectedResult.Pass)]
[CombinedDataSources]
public class ClassLevelCombinedDataSources_ThreeParameters(
    [Arguments(1, 2)] int x,
    [Arguments("a", "b")] string y,
    [Arguments(true, false)] bool z)
{
    [Test]
    public async Task Test_ShouldHandleThreeParameters()
    {
        // Should create 2 x 2 x 2 = 8 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn(["a", "b"]);
        await Assert.That(z).IsIn([true, false]);
    }
}

#endregion

#region Edge Case: Instance data source at class level

/// <summary>
/// Edge case documentation: When using CombinedDataSources at the class level with
/// an instance-requiring data source (like MethodDataSource pointing to an instance method),
/// the runtime check in CombinedDataSourcesAttribute.GetParameterValues() (lines 156-165)
/// will throw an InvalidOperationException with a clear, diagnostic error message:
///
/// "Cannot use instance-based data source '{AttributeName}' on parameter '{name}' in class '{ClassName}'.
/// When [CombinedDataSources] is applied at the class level (constructor parameters), all data sources
/// must be static because no instance exists yet. Use static [MethodDataSource] or [Arguments] instead,
/// or move [CombinedDataSources] to the method level if you need instance-based data sources."
///
/// This provides proper error handling with actionable guidance, replacing the confusing
/// "circular dependency" message that was previously shown due to the blanket IAccessesInstanceData check.
/// </summary>
public class EdgeCaseDocumentation;

#endregion
