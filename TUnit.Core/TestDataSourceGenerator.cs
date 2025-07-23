using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Enums;

namespace TUnit.Core;

/// <summary>
/// Runtime helper for generating data from typed data sources for generic tests
/// </summary>
public static class TestDataSourceGenerator
{
    /// <summary>
    /// Generate a single data value from a typed data source at runtime
    /// </summary>
    public static async Task<object?> GenerateTypedDataSourceValueAsync(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type dataSourceType,
        string testSessionId,
        Dictionary<string, Type> resolvedGenericTypes)
    {
        try
        {
            // Create an instance of the data source
            var dataSource = Activator.CreateInstance(dataSourceType);
            
            if (dataSource is not IDataSourceAttribute asyncDataSource)
            {
                throw new InvalidOperationException(
                    $"Data source {dataSourceType.Name} does not implement IDataSourceAttribute");
            }
            
            // Create metadata for the data source with resolved generic types
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
                MembersToGenerate = [
                ],
                TestInformation = new MethodMetadata
                {
                    Name = "GenericTest",
                    Type = typeof(object),
                    GenericTypeCount = 0,
                    Parameters = [
                    ],
                    ReturnType = typeof(Task),
                    ReturnTypeReference = TypeReference.CreateConcrete("System.Threading.Tasks.Task, System.Runtime"),
                    TypeReference = TypeReference.CreateConcrete("System.Object, System.Runtime"),
                    Class = new ClassMetadata
                    {
                        Type = typeof(object),
                        Name = "GenericTestClass",
                        Namespace = "TUnit.TestProject",
                        Parent = null,
                        TypeReference = TypeReference.CreateConcrete("System.Object, System.Runtime"),
                        Assembly = new AssemblyMetadata { Name = "TUnit.TestProject" },
                        Parameters = [
                        ],
                        Properties = [
                        ]
                    }
                },
                Type = DataGeneratorType.TestParameters,
                TestSessionId = testSessionId,
                TestClassInstance = null,
                ClassInstanceArguments = null
            };
            
            // Get the first data value from the typed data source
            await foreach (var factory in asyncDataSource.GetDataRowsAsync(metadata))
            {
                var data = await factory();
                return data?.Length > 0 ? data[0] : null;
            }
            
            // If no data was generated, return null
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to generate data from typed data source {dataSourceType.Name}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Generate a single data value from Matrix-based data sources at runtime
    /// </summary>
    [RequiresUnreferencedCode("MatrixDataSourceAttribute may require unreferenced code for enum reflection and matrix generation. This attribute is inherently incompatible with AOT compilation.")]
    [RequiresDynamicCode("MatrixDataSourceAttribute requires dynamic code generation for runtime matrix generation and enum reflection. This attribute is inherently incompatible with AOT compilation.")]
    public static async Task<object?> GenerateMatrixDataSourceValueAsync(
        string testSessionId,
        Dictionary<string, Type> resolvedGenericTypes)
    {
        try
        {
            // Create a MatrixDataSource instance
            var dataSource = new MatrixDataSourceAttribute();
            
            // Create metadata for the Matrix data source
            var metadata = new DataGeneratorMetadata
            {
                TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext()),
                MembersToGenerate = [
                ],
                TestInformation = new MethodMetadata
                {
                    Name = "GenericMatrixTest",
                    Type = typeof(object),
                    GenericTypeCount = 0,
                    Parameters = [
                    ],
                    ReturnType = typeof(Task),
                    ReturnTypeReference = TypeReference.CreateConcrete("System.Threading.Tasks.Task, System.Runtime"),
                    TypeReference = TypeReference.CreateConcrete("System.Object, System.Runtime"),
                    Class = new ClassMetadata
                    {
                        Type = typeof(object),
                        Name = "GenericMatrixTestClass",
                        Namespace = "TUnit.TestProject",
                        Parent = null,
                        TypeReference = TypeReference.CreateConcrete("System.Object, System.Runtime"),
                        Assembly = new AssemblyMetadata { Name = "TUnit.TestProject" },
                        Parameters = [
                        ],
                        Properties = [
                        ]
                    }
                },
                Type = DataGeneratorType.TestParameters,
                TestSessionId = testSessionId,
                TestClassInstance = null,
                ClassInstanceArguments = null
            };
            
            // Get the first data value from the Matrix data source
            await foreach (var factory in dataSource.GetDataRowsAsync(metadata))
            {
                var data = await factory();
                return data?.Length > 0 ? data[0] : null;
            }
            
            // If no data was generated, return null
            return null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Failed to generate data from Matrix data source: {ex.Message}", ex);
        }
    }
}