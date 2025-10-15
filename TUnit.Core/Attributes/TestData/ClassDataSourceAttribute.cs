using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] T>
    : DataSourceGeneratorAttribute<T>
{
    public SharedType Shared { get; set; } = SharedType.None;
    public string Key { get; set; } = string.Empty;
    public Type ClassType => typeof(T);

    protected override IEnumerable<Func<T>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        var testClassType = TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);
        yield return () => ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
            .Get<T>(Shared, testClassType, Key, dataGeneratorMetadata);
    }


    public IEnumerable<SharedType> GetSharedTypes() => [Shared];

    public IEnumerable<string> GetKeys() => string.IsNullOrEmpty(Key) ? [] : [Key];
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property, AllowMultiple = true)]
#if NET6_0_OR_GREATER
[RequiresUnreferencedCode("ClassDataSourceAttribute uses reflection to instantiate and access test data classes. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
[RequiresDynamicCode("ClassDataSourceAttribute may require runtime type generation. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
#endif
public sealed class ClassDataSourceAttribute : UntypedDataSourceGeneratorAttribute
{
    private readonly Type[] _types;

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("ClassDataSourceAttribute uses reflection to instantiate and access test data classes. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    [RequiresDynamicCode("ClassDataSourceAttribute may require runtime type generation. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    #endif
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type) : this([type])
    {
    }

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("ClassDataSourceAttribute uses reflection to instantiate and access test data classes. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    [RequiresDynamicCode("ClassDataSourceAttribute may require runtime type generation. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    #endif
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type2) : this([type, type2])
    {
    }

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("ClassDataSourceAttribute uses reflection to instantiate and access test data classes. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    [RequiresDynamicCode("ClassDataSourceAttribute may require runtime type generation. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    #endif
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type3) : this([type, type2, type3])
    {
    }

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("ClassDataSourceAttribute uses reflection to instantiate and access test data classes. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    [RequiresDynamicCode("ClassDataSourceAttribute may require runtime type generation. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    #endif
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type4) : this([type, type2, type3, type4])
    {
    }

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("ClassDataSourceAttribute uses reflection to instantiate and access test data classes. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    [RequiresDynamicCode("ClassDataSourceAttribute may require runtime type generation. For AOT compatibility, use strongly-typed ClassDataSourceAttribute<T> instead.")]
    #endif
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type4,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        Type type5) : this([type, type2, type3, type4, type5])
    {
    }

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Reflection")]
    [RequiresDynamicCode("Reflection")]
    #endif
    public ClassDataSourceAttribute(params Type[] types)
    {
        _types = types;
    }

    public SharedType[] Shared { get; set; } = [SharedType.None];
    public string[] Keys { get; set; } = [];

    #if NET6_0_OR_GREATER
    [RequiresUnreferencedCode("Reflection")]
    [RequiresDynamicCode("Reflection")]
    #endif
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            var items = new object?[_types.Length];

            for (var i = 0; i < _types.Length; i++)
            {
                var testClassType = TestClassTypeHelper.GetTestClassType(dataGeneratorMetadata);
                items[i] = ClassDataSources.Get(dataGeneratorMetadata.TestSessionId)
                    .Get(Shared.ElementAtOrDefault(i), _types[i], testClassType, Keys.ElementAtOrDefault(i), dataGeneratorMetadata);
            }

            return items;
        };
    }

    public IEnumerable<SharedType> GetSharedTypes() => Shared;

    public IEnumerable<string> GetKeys() => Keys;

}
