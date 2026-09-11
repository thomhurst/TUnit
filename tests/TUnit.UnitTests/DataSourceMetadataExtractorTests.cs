using TUnit.Engine.Helpers;

namespace TUnit.UnitTests;

public class DataSourceMetadataExtractorTests
{
    [Test]
    public async Task NullDataSource_ReturnsNull()
    {
        await Assert.That(DataSourceMetadataExtractor.ExtractFromAttribute(null)).IsNull();
    }

    [Test]
    public async Task CustomDataSource_PreservesReflectionFallback()
    {
        var source = new CustomDataSource { DisplayName = "custom", Skip = "skip", Categories = ["category"] };
        var metadata = DataSourceMetadataExtractor.ExtractFromAttribute(source)!;

        await Assert.That(metadata.DisplayName).IsEqualTo(source.DisplayName);
        await Assert.That(metadata.Skip).IsEqualTo(source.Skip);
        await Assert.That(metadata.Categories).IsSameReferenceAs(source.Categories);
    }

    [Test]
    public async Task ArgumentsWithoutMetadata_ReturnsNull()
    {
        await Assert.That(DataSourceMetadataExtractor.ExtractFromAttribute(new ArgumentsAttribute(42))).IsNull();
    }

    [Test]
    public async Task ArgumentsWithMetadata_PreservesAllValues()
    {
        var arguments = new ArgumentsAttribute(42)
        {
            DisplayName = "row name",
            Skip = "row skip",
            Categories = ["one", "two"]
        };

        var metadata = DataSourceMetadataExtractor.ExtractFromAttribute(arguments)!;

        await Assert.That(metadata.DisplayName).IsEqualTo(arguments.DisplayName);
        await Assert.That(metadata.Skip).IsEqualTo(arguments.Skip);
        await Assert.That(metadata.Categories).IsSameReferenceAs(arguments.Categories);
        await Assert.That(metadata.DataExpression).IsNull();
    }

    [Test]
    public async Task ArgumentsWithEmptyMetadata_DoesNotTreatEmptyAsNull()
    {
        var arguments = new ArgumentsAttribute(42)
        {
            DisplayName = "",
            Skip = "",
            Categories = []
        };

        var metadata = DataSourceMetadataExtractor.ExtractFromAttribute(arguments)!;

        await Assert.That(metadata.DisplayName).IsEqualTo("");
        await Assert.That(metadata.Skip).IsEqualTo("");
        await Assert.That(metadata.Categories).IsSameReferenceAs(arguments.Categories);
    }

    [Test]
    public async Task ArgumentsMetadata_IsReadAgainAfterMutation()
    {
        var arguments = new ArgumentsAttribute(42);
        await Assert.That(DataSourceMetadataExtractor.ExtractFromAttribute(arguments)).IsNull();

        arguments.Skip = "updated";
        await Assert.That(DataSourceMetadataExtractor.ExtractFromAttribute(arguments)!.Skip).IsEqualTo("updated");

        arguments.Skip = null;
        await Assert.That(DataSourceMetadataExtractor.ExtractFromAttribute(arguments)).IsNull();
    }

    private sealed class CustomDataSource : IDataSourceAttribute
    {
        public string? DisplayName { get; set; }
        public string? Skip { get; set; }
        public string[]? Categories { get; set; }
        public bool SkipIfEmpty { get; set; }
        public bool DeferEnumeration { get; set; }

        public IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
            => throw new NotSupportedException();
    }
}
