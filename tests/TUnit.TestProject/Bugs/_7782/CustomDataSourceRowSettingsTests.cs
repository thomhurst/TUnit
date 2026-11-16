using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7782;

// A custom data source's DisplayName, Skip and Categories properties apply to its rows in every mode.
// Nothing but the engine reads those getters, so under Native AOT the trimmer removes them unless the
// source generator emits the read.

public class RowSettingsSourceAttribute : DataSourceGeneratorAttribute<int>
{
    public string? DisplayName { get; set; }

    public string? Skip { get; set; }

    public string[]? Categories { get; set; }

    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 7;
    }
}

// The settings come from the constructor and a computed getter, not from named arguments at the usage site.
public class ComputedRowSettingsSourceAttribute(string name) : RowSettingsSourceAttribute
{
    public new string DisplayName => $"computed {name}";
}

public class GenericRowSettingsSourceAttribute<T> : DataSourceGeneratorAttribute<T>
    where T : new()
{
    public string? DisplayName { get; set; }

    protected override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => new T();
    }
}

[EngineTest(ExpectedResult.Pass)]
public class CustomDataSourceRowSettings
{
    [Test, RowSettingsSource(DisplayName = "seven from a custom source", Categories = ["Issue7782Category"])]
    public async Task Named(int value)
    {
        await AssertRow(value, "seven from a custom source", hasCategory: true);
    }

    [Test, RowSettingsSource(Skip = "Skipped by a custom source")]
    public void Skipped(int value)
    {
        Assert.Fail("A row whose data source sets Skip must not run");
    }

    [Test, RowSettingsSource]
    public async Task Unset(int value)
    {
        await Assert.That(value).IsEqualTo(7);
        await Assert.That(TestContext.Current!.Metadata.TestDetails.Categories).DoesNotContain("Issue7782Category");
    }

    [Test, ComputedRowSettingsSource("name", Categories = ["Issue7782Category"])]
    public async Task Computed(int value)
    {
        await AssertRow(value, "computed name", hasCategory: true);
    }

    [Test, GenericRowSettingsSource<int>(DisplayName = "generic source")]
    public async Task Generic(int value)
    {
        await AssertRow(value, "generic source", hasCategory: false, expectedValue: 0);
    }

    private static async Task AssertRow(int value, string displayName, bool hasCategory, int expectedValue = 7)
    {
        var context = TestContext.Current!;

        await Assert.That(value).IsEqualTo(expectedValue);
        await Assert.That(context.Metadata.DisplayName).IsEqualTo(displayName);
        await Assert.That(context.Metadata.TestDetails.Categories.Contains("Issue7782Category")).IsEqualTo(hasCategory);
    }
}

[EngineTest(ExpectedResult.Pass)]
[RowSettingsSource(DisplayName = "class row from a custom source")]
public class CustomClassDataSourceRowSettings(int value)
{
    [Test]
    public async Task Run()
    {
        await Assert.That(value).IsEqualTo(7);
        await Assert.That(TestContext.Current!.Metadata.DisplayName).IsEqualTo("class row from a custom source");
    }
}
