using System.Diagnostics.CodeAnalysis;
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

public class HiddenRowSettingsBaseAttribute : DataSourceGeneratorAttribute<int>
{
    public string? DisplayName => "from the hidden base property";

    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 7;
    }
}

// A field hides the base property from a member access, but not from Type.GetProperty.
public class FieldHidesRowSettingSourceAttribute : HiddenRowSettingsBaseAttribute
{
    public new int DisplayName = 1;
}

// A property without a getter is ignored; the readable one still applies.
public class WriteOnlySkipSourceAttribute : DataSourceGeneratorAttribute<int>
{
    public string? DisplayName { get; set; }

    public string? Skip { set { } }

    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 7;
    }
}

// Properties of other types do not stop the readable name from applying. An object can hold the setting.
public class OtherTypedRowSettingsSourceAttribute : DataSourceGeneratorAttribute<int>
{
    public object? DisplayName { get; set; }

    public bool Skip { get; set; }

    public IReadOnlyList<string> Categories => new List<string> { "Issue7782Category" };

    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 7;
    }
}

public class ObjectNamedRowSettingsBaseAttribute : DataSourceGeneratorAttribute<int>
{
    public object DisplayName => "from the base property";

    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 7;
    }
}

// The most derived property wins, also when it hides a property of another type.
public class RetypedRowSettingSourceAttribute : ObjectNamedRowSettingsBaseAttribute
{
    public new string DisplayName => "from the retyped property";
}

public class StaticRowSettingSourceAttribute : DataSourceGeneratorAttribute<int>
{
    public static string DisplayName => "from a static property";

    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 7;
    }
}

// Generated code cannot read Skip, so a usage that never mentions it must still compile. The attribute is then
// read by reflection, which under Native AOT needs the getters kept.
public class ObsoleteSkipSourceAttribute : DataSourceGeneratorAttribute<int>
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.PublicProperties, typeof(ObsoleteSkipSourceAttribute))]
    public ObsoleteSkipSourceAttribute()
    {
    }

    public string? DisplayName { get; set; }

    [Obsolete("Use DisplayName", error: true)]
    public string? Skip { get; set; }

    protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () => 7;
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

    [Test, FieldHidesRowSettingSource]
    public async Task HiddenByField(int value)
    {
        await AssertRow(value, "from the hidden base property", hasCategory: false);
    }

    [Test, WriteOnlySkipSource(DisplayName = "readable name")]
    public async Task WriteOnlySkip(int value)
    {
        await AssertRow(value, "readable name", hasCategory: false);
    }

    [Test, OtherTypedRowSettingsSource(DisplayName = "name held as an object", Skip = true)]
    public async Task OtherTypes(int value)
    {
        await AssertRow(value, "name held as an object", hasCategory: false);
    }

    [Test, RetypedRowSettingSource]
    public async Task Retyped(int value)
    {
        await AssertRow(value, "from the retyped property", hasCategory: false);
    }

    [Test, StaticRowSettingSource]
    public async Task Static(int value)
    {
        await AssertRow(value, "from a static property", hasCategory: false);
    }

    [Test, ObsoleteSkipSource(DisplayName = "name beside an obsolete setting")]
    public async Task ObsoleteSkip(int value)
    {
        await AssertRow(value, "name beside an obsolete setting", hasCategory: false);
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
