using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Helpers;

namespace TUnit.Core;

/// <summary>
/// Provides test data by creating instances of one or more specified types.
/// The instances are created using their constructors and can optionally be shared across tests.
/// </summary>
/// <remarks>
/// <para>
/// Use this attribute to inject class instances as test method parameters or constructor arguments.
/// The attribute supports sharing instances across tests via the <see cref="Shared"/> property and
/// keyed sharing via the <see cref="Keys"/> property.
/// </para>
/// <para>
/// For strongly-typed single-parameter usage, prefer the generic version <see cref="ClassDataSourceAttribute{T}"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create a new instance for each test
/// [Test]
/// [ClassDataSource(typeof(MyService))]
/// public void TestWithService(MyService service) { }
///
/// // Share the instance across all tests in the class
/// [Test]
/// [ClassDataSource(typeof(MyService), Shared = [SharedType.PerClass])]
/// public void TestWithSharedService(MyService service) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute : UntypedDataSourceGeneratorAttribute
{
    private Type[] _types;

    public ClassDataSourceAttribute()
    {
        _types = [];
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Non-params constructor calls params one with proper annotations.")]
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type) : this([type])
    {
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Non-params constructor calls params one with proper annotations.")]
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type2) : this([type, type2])
    {
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Non-params constructor calls params one with proper annotations.")]
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type3) : this([type, type2, type3])
    {
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Non-params constructor calls params one with proper annotations.")]
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type4) : this([type, type2, type3, type4])
    {
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", Justification = "Non-params constructor calls params one with proper annotations.")]
    public ClassDataSourceAttribute(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type2,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type3,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type4,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
        Type type5) : this([type, type2, type3, type4, type5])
    {
    }

    [RequiresUnreferencedCode("Uses dynamically accessed types.")]
    public ClassDataSourceAttribute(params Type[] types)
    {
        _types = types;
    }

    /// <summary>
    /// Gets or sets how instances are shared across tests, one per type parameter.
    /// Defaults to <see cref="SharedType.None"/> (a new instance per test).
    /// </summary>
    public SharedType[] Shared { get; set; } = [SharedType.None];

    /// <summary>
    /// Gets or sets the sharing keys, one per type parameter.
    /// Used when <see cref="Shared"/> is set to <see cref="SharedType.Keyed"/> to identify shared instances.
    /// </summary>
    public string[] Keys { get; set; } = [];

    [UnconditionalSuppressMessage("Trimming", "IL2062:The parameter of method has a DynamicallyAccessedMembersAttribute, but the value passed to it can not be statically analyzed.",
        Justification = "Constructor parameter is annotated with DynamicallyAccessedMembers, so _types elements have the required annotations.")]
    protected override IEnumerable<Func<object?[]?>> GenerateDataSources(DataGeneratorMetadata dataGeneratorMetadata)
    {
        yield return () =>
        {
            if (_types.Length == 0)
            {
                _types = dataGeneratorMetadata.MembersToGenerate.Select(x =>
                {
                    if (x is ParameterMetadata parameterMetadata)
                    {
                        return parameterMetadata.Type;
                    }

                    if (x is PropertyMetadata propertyMetadata)
                    {
                        return propertyMetadata.Type;
                    }

                    throw new ArgumentOutOfRangeException(nameof(dataGeneratorMetadata),
                        "Member to generate must be either a parameter or a property.");
                }).ToArray();
            }

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

/// <summary>
/// Provides test data by creating an instance of <typeparamref name="T"/>.
/// The instance is created using its constructor and can optionally be shared across tests.
/// </summary>
/// <typeparam name="T">The type to instantiate as test data.</typeparam>
/// <remarks>
/// <para>
/// Use the <see cref="Shared"/> property to control instance sharing:
/// <see cref="SharedType.None"/> (default) creates a new instance per test,
/// <see cref="SharedType.PerClass"/> shares within the test class,
/// <see cref="SharedType.PerAssembly"/> shares across the assembly,
/// <see cref="SharedType.Keyed"/> shares by a specified <see cref="Key"/>.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// [ClassDataSource&lt;MyService&gt;(Shared = SharedType.PerClass)]
/// public void TestWithService(MyService service) { }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Parameter, AllowMultiple = true)]
public sealed class ClassDataSourceAttribute<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] T>
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
