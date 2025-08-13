using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using TUnit.Core.Enums;

namespace TUnit.Core;

internal static class DataGeneratorMetadataCreator
{
    public static DataGeneratorMetadata CreateDataGeneratorMetadata(
        TestMetadata testMetadata,
        string testSessionId,
        DataGeneratorType generatorType,
        object? testClassInstance,
        object?[]? classInstanceArguments,
        TestBuilderContextAccessor contextAccessor)
    {
        // Determine which parameters we're generating for
        var parametersToGenerate = generatorType == DataGeneratorType.ClassParameters
            ? testMetadata.MethodMetadata.Class.Parameters
            : testMetadata.MethodMetadata.Parameters;

        // Filter out CancellationToken if it's the last parameter (handled by the engine)
        if (generatorType == DataGeneratorType.TestParameters && parametersToGenerate.Length > 0)
        {
            var lastParam = parametersToGenerate[parametersToGenerate.Length - 1];
            if (lastParam.Type == typeof(System.Threading.CancellationToken))
            {
                var newArray = new ParameterMetadata[parametersToGenerate.Length - 1];
                Array.Copy(parametersToGenerate, 0, newArray, 0, parametersToGenerate.Length - 1);
                parametersToGenerate = newArray;
            }
        }

        // Handle property data generation specifically
        MemberMetadata[] membersToGenerate;
        if (generatorType == DataGeneratorType.Property)
        {
            // For properties, we generate data for properties that have data sources
            // If PropertyDataSources is populated, use only those properties
            if (testMetadata.PropertyDataSources.Length > 0)
            {
                var propertyMetadataList = new List<PropertyMetadata>();
                var allProperties = testMetadata.MethodMetadata.Class.Properties;

                foreach (var propertyDataSource in testMetadata.PropertyDataSources)
                {
                    var matchingProperty = allProperties.FirstOrDefault(p => p.Name == propertyDataSource.PropertyName);
                    if (matchingProperty != null)
                    {
                        propertyMetadataList.Add(matchingProperty);
                    }
                }

                membersToGenerate = [.. propertyMetadataList];
            }
            else
            {
                // If no specific PropertyDataSources, include all class properties
                membersToGenerate = testMetadata.MethodMetadata.Class.Properties;
            }
        }
        else
        {
            // For parameters (class or test), use the parameter metadata
            membersToGenerate = [..parametersToGenerate];
        }

        return new DataGeneratorMetadata
        {
            TestBuilderContext = contextAccessor,
            MembersToGenerate = membersToGenerate,
            TestInformation = testMetadata.MethodMetadata,
            Type = generatorType,
            TestSessionId = testSessionId,
            TestClassInstance = testClassInstance,
            ClassInstanceArguments = classInstanceArguments
        };
    }

    /// <summary>
    /// Creates DataGeneratorMetadata for reflection-based test discovery.
    /// Used when discovering tests and expanding data sources during the discovery phase.
    /// </summary>
    public static DataGeneratorMetadata CreateForReflectionDiscovery(
        MethodMetadata methodMetadata,
        DataGeneratorType generatorType,
        string testSessionId = "reflection-discovery")
    {
        // Determine which members we're generating for based on type
        MemberMetadata[] membersToGenerate = generatorType switch
        {
            DataGeneratorType.ClassParameters => methodMetadata.Class.Parameters,
            DataGeneratorType.TestParameters => FilterOutCancellationToken(methodMetadata.Parameters),
            DataGeneratorType.Property => methodMetadata.Class.Properties,
            _ => []
        };

        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
            {
                TestMetadata = null!, // Not available during discovery
                Events = new TestContextEvents(),
                ObjectBag = new Dictionary<string, object?>()
            }),
            MembersToGenerate = membersToGenerate,
            TestInformation = methodMetadata,
            Type = generatorType,
            TestSessionId = testSessionId,
            TestClassInstance = null,
            ClassInstanceArguments = null
        };
    }

    /// <summary>
    /// Creates minimal DataGeneratorMetadata for discovery phase when inferring generic types.
    /// This is used when we need to get data from sources to determine generic type arguments.
    /// </summary>
    public static DataGeneratorMetadata CreateForGenericTypeDiscovery(
        IDataSourceAttribute dataSource,
        MethodMetadata? existingMethodMetadata = null)
    {
        var dummyParameter = new ParameterMetadata(typeof(object))
        {
            Name = "param0",
            TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName },
            ReflectionInfo = null!
        };

        var discoveryMethodMetadata = existingMethodMetadata ?? new MethodMetadata
        {
            Name = "Discovery",
            Type = typeof(object),
            Class = ClassMetadata.GetOrAdd("Discovery", () => new ClassMetadata
            {
                Name = "Discovery",
                Type = typeof(object),
                Namespace = string.Empty,
                TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName },
                Assembly = AssemblyMetadata.GetOrAdd("Discovery", () => new AssemblyMetadata { Name = "Discovery" }),
                Parameters = [dummyParameter],
                Properties = [],
                Parent = null
            }),
            Parameters = [],
            GenericTypeCount = 0,
            ReturnTypeReference = new TypeReference { AssemblyQualifiedName = typeof(void).AssemblyQualifiedName },
            ReturnType = typeof(void),
            TypeReference = new TypeReference { AssemblyQualifiedName = typeof(object).AssemblyQualifiedName }
        };

        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(new TestBuilderContext
            {
                TestMetadata = discoveryMethodMetadata,
                DataSourceAttribute = dataSource,
                Events = new TestContextEvents(),
                ObjectBag = new Dictionary<string, object?>()
            }),
            MembersToGenerate = [dummyParameter],
            TestInformation = discoveryMethodMetadata,
            Type = DataGeneratorType.ClassParameters,
            TestSessionId = "discovery",
            TestClassInstance = null,
            ClassInstanceArguments = null
        };
    }

    /// <summary>
    /// Creates DataGeneratorMetadata for property injection scenarios.
    /// </summary>
    public static DataGeneratorMetadata CreateForPropertyInjection(
        PropertyMetadata propertyMetadata,
        MethodMetadata? methodMetadata,
        IDataSourceAttribute dataSource,
        TestContext? testContext = null,
        object? testClassInstance = null,
        TestContextEvents? events = null,
        Dictionary<string, object?>? objectBag = null)
    {
        var testBuilderContext = testContext != null
            ? TestBuilderContext.FromTestContext(testContext, dataSource)
            : methodMetadata != null
                ? new TestBuilderContext
                {
                    Events = events ?? new TestContextEvents(),
                    TestMetadata = methodMetadata,
                    DataSourceAttribute = dataSource,
                    ObjectBag = objectBag ?? []
                }
                : TestSessionContext.GlobalStaticPropertyContext;

        return new DataGeneratorMetadata
        {
            TestBuilderContext = new TestBuilderContextAccessor(testBuilderContext),
            MembersToGenerate = [propertyMetadata],
            TestInformation = methodMetadata,
            Type = DataGeneratorType.Property,
            TestSessionId = TestSessionContext.Current?.Id ?? "property-injection",
            TestClassInstance = testClassInstance ?? testContext?.TestDetails.ClassInstance,
            ClassInstanceArguments = testContext?.TestDetails.TestClassArguments ?? []
        };
    }

    /// <summary>
    /// Creates DataGeneratorMetadata for property injection using PropertyInfo (reflection mode).
    /// </summary>
    [UnconditionalSuppressMessage("Trimming", "IL2072:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.PropertyMetadata.Type.init'", Justification = "Property types are resolved through reflection")]
    public static DataGeneratorMetadata CreateForPropertyInjection(
        PropertyInfo property,
        Type containingType,
        MethodMetadata? methodMetadata,
        IDataSourceAttribute dataSource,
        TestContext? testContext = null,
        object? testClassInstance = null,
        TestContextEvents? events = null,
        Dictionary<string, object?>? objectBag = null)
    {
        var propertyMetadata = new PropertyMetadata
        {
            IsStatic = property.GetMethod?.IsStatic ?? false,
            Name = property.Name,
            ClassMetadata = GetClassMetadataForType(containingType),
            Type = property.PropertyType,
            ReflectionInfo = property,
            Getter = parent => property.GetValue(parent),
            ContainingTypeMetadata = GetClassMetadataForType(containingType)
        };

        return CreateForPropertyInjection(
            propertyMetadata,
            methodMetadata,
            dataSource,
            testContext,
            testClassInstance,
            events,
            objectBag);
    }

    private static ParameterMetadata[] FilterOutCancellationToken(ParameterMetadata[] parameters)
    {
        if (parameters.Length > 0)
        {
            var lastParam = parameters[parameters.Length - 1];
            if (lastParam.Type == typeof(System.Threading.CancellationToken))
            {
                var newArray = new ParameterMetadata[parameters.Length - 1];
                Array.Copy(parameters, 0, newArray, 0, parameters.Length - 1);
                return newArray;
            }
        }
        return parameters;
    }

    [UnconditionalSuppressMessage("Trimming", "IL2070:'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' in call to 'System.Type.GetConstructors(BindingFlags)'", Justification = "Constructor discovery needed for metadata")]
    [UnconditionalSuppressMessage("Trimming", "IL2067:'value' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ClassMetadata.Type.init'", Justification = "Type annotations are handled by reflection")]
    [UnconditionalSuppressMessage("Trimming", "IL2072:'Type' argument does not satisfy 'DynamicallyAccessedMemberTypes' in call to 'TUnit.Core.ParameterMetadata.ParameterMetadata(Type)'", Justification = "Parameter types are known through reflection")]
    private static ClassMetadata GetClassMetadataForType(Type type)
    {
        return ClassMetadata.GetOrAdd(type.FullName ?? type.Name, () =>
        {
            var constructors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            var constructor = constructors.FirstOrDefault();

            var constructorParameters = constructor?.GetParameters().Select((p, i) => new ParameterMetadata(p.ParameterType)
            {
                Name = p.Name ?? $"param{i}",
                TypeReference = new TypeReference { AssemblyQualifiedName = p.ParameterType.AssemblyQualifiedName },
                ReflectionInfo = p
            }).ToArray() ?? Array.Empty<ParameterMetadata>();

            return new ClassMetadata
            {
                Type = type,
                TypeReference = TypeReference.CreateConcrete(type.AssemblyQualifiedName ?? type.FullName ?? type.Name),
                Name = type.Name,
                Namespace = type.Namespace ?? string.Empty,
                Assembly = AssemblyMetadata.GetOrAdd(type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown", () => new AssemblyMetadata
                {
                    Name = type.Assembly.GetName().Name ?? type.Assembly.GetName().FullName ?? "Unknown"
                }),
                Properties = [],
                Parameters = constructorParameters,
                Parent = type.DeclaringType != null ? GetClassMetadataForType(type.DeclaringType) : null
            };
        });
    }
}
