namespace TUnit.UnitTests;

/// <summary>
/// Unit tests to verify that empty InstanceMethodDataSource yields exactly one empty result
/// This ensures the fix for issue #2862 works correctly
/// </summary>
public class EmptyDataSourceTests
{
    public IEnumerable<int> EmptyData => [];
    public IEnumerable<int> NonEmptyData => [1, 2, 3];
    
    public static IEnumerable<string> StaticEmptyData => [];
    public static IEnumerable<string> StaticNonEmptyData => ["a", "b"];

    /// <summary>
    /// Test the InstanceMethodDataSourceAttribute directly to ensure it yields one empty result for empty enumerable
    /// </summary>
    [Test]
    public async Task EmptyInstanceMethodDataSource_ShouldYieldOneEmptyResult()
    {
        // Arrange
        var attribute = new InstanceMethodDataSourceAttribute(nameof(EmptyData));
        var metadata = new DataGeneratorMetadata
        {
            Type = typeof(EmptyDataSourceTests),
            TestClassInstance = this,
            MembersToGenerate = new IMetadata[]
            {
                new MethodMetadata
                {
                    ClassMetadata = new ClassMetadata { Type = typeof(EmptyDataSourceTests) },
                    Parameters = []
                }
            }
        };

        // Act
        var results = new List<Func<Task<object?[]?>>>();
        await foreach (var factory in attribute.GetDataRowsAsync(metadata))
        {
            results.Add(factory);
        }

        // Assert
        await Assert.That(results).HasCount().EqualTo(1); // Should yield exactly one result
        
        var data = await results[0]();
        await Assert.That(data).IsNotNull();
        await Assert.That(data).HasCount().EqualTo(0); // Should be empty array
    }

    /// <summary>
    /// Test non-empty instance method data source still works correctly
    /// </summary>
    [Test]
    public async Task NonEmptyInstanceMethodDataSource_ShouldYieldMultipleResults()
    {
        // Arrange
        var attribute = new InstanceMethodDataSourceAttribute(nameof(NonEmptyData));
        var metadata = new DataGeneratorMetadata
        {
            Type = typeof(EmptyDataSourceTests),
            TestClassInstance = this,
            MembersToGenerate = new IMetadata[]
            {
                new MethodMetadata
                {
                    ClassMetadata = new ClassMetadata { Type = typeof(EmptyDataSourceTests) },
                    Parameters = []
                }
            }
        };

        // Act
        var results = new List<Func<Task<object?[]?>>>();
        await foreach (var factory in attribute.GetDataRowsAsync(metadata))
        {
            results.Add(factory);
        }

        // Assert
        await Assert.That(results).HasCount().EqualTo(3); // Should yield three results (1, 2, 3)
    }

    /// <summary>
    /// Test that static empty method data source behaves correctly
    /// </summary>
    [Test]
    public async Task EmptyStaticMethodDataSource_ShouldYieldOneEmptyResult()
    {
        // Arrange
        var attribute = new MethodDataSourceAttribute(typeof(EmptyDataSourceTests), nameof(StaticEmptyData));
        var metadata = new DataGeneratorMetadata
        {
            Type = typeof(EmptyDataSourceTests),
            TestClassInstance = null, // Static method
            MembersToGenerate = new IMetadata[]
            {
                new MethodMetadata
                {
                    ClassMetadata = new ClassMetadata { Type = typeof(EmptyDataSourceTests) },
                    Parameters = []
                }
            }
        };

        // Act
        var results = new List<Func<Task<object?[]?>>>();
        await foreach (var factory in attribute.GetDataRowsAsync(metadata))
        {
            results.Add(factory);
        }

        // Assert
        await Assert.That(results).HasCount().EqualTo(1); // Should yield exactly one result
        
        var data = await results[0]();
        await Assert.That(data).IsNotNull();
        await Assert.That(data).HasCount().EqualTo(0); // Should be empty array
    }

    /// <summary>
    /// Compare behavior with NoDataSource to ensure consistency
    /// </summary>
    [Test]
    public async Task NoDataSource_ShouldYieldOneEmptyResult()
    {
        // Arrange
        var noDataSource = NoDataSource.Instance;
        var metadata = new DataGeneratorMetadata
        {
            Type = typeof(EmptyDataSourceTests),
            TestClassInstance = this,
            MembersToGenerate = new IMetadata[]
            {
                new MethodMetadata
                {
                    ClassMetadata = new ClassMetadata { Type = typeof(EmptyDataSourceTests) },
                    Parameters = []
                }
            }
        };

        // Act
        var results = new List<Func<Task<object?[]?>>>();
        await foreach (var factory in noDataSource.GetDataRowsAsync(metadata))
        {
            results.Add(factory);
        }

        // Assert
        await Assert.That(results).HasCount().EqualTo(1); // NoDataSource yields exactly one result
        
        var data = await results[0]();
        await Assert.That(data).IsNotNull();
        await Assert.That(data).HasCount().EqualTo(0); // Should be empty array
    }
}