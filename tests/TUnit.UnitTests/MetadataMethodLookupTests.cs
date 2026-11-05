using TUnit.Core.Extensions;

namespace TUnit.UnitTests;

public class MetadataMethodLookupTests
{
    [Test]
    [Arguments(false)]
    [Arguments(true)]
    public async Task Open_class_parameter_shape_distinguishes_object(bool generic)
    {
        TUnit.Core.TypeInfo shape = generic ? new GenericParameter(0, false) : new ConcreteType(typeof(object));
        var method = Create("Run", 0, shape).GetReflectionInfo();
        await Assert.That(method.GetParameters()[0].ParameterType.IsGenericParameter).IsEqualTo(generic);
    }

    [Test]
    [Arguments(0)]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Zero_parameter_overloads_match_generic_arity(int arity)
    {
        var method = Create("Empty", arity).GetReflectionInfo();
        await Assert.That(method.GetGenericArguments().Length).IsEqualTo(arity);
    }

    [Test]
    public async Task Constructed_parameter_shapes_match_recursively()
    {
        var shape = new ConstructedGeneric(typeof(List<>), [new GenericParameter(0, false)]);
        var method = Create("Nested", 0, shape).GetReflectionInfo();
        await Assert.That(method.GetParameters()[0].ParameterType.GetGenericArguments()[0].IsGenericParameter).IsTrue();
    }

    [Test]
    public async Task Older_erased_array_metadata_keeps_unambiguous_lookup()
    {
        var method = Create("Array", 0, new ConcreteType(typeof(object))).GetReflectionInfo();
        await Assert.That(method.GetParameters()[0].ParameterType.IsArray).IsTrue();
    }

    [Test]
    public async Task Reflection_metadata_preserves_closed_generic_method()
    {
        var reflected = typeof(Overloads<string>).GetMethod("Empty", 1, System.Type.EmptyTypes)!
            .MakeGenericMethod(typeof(int));
        var metadata = TUnit.Engine.Building.ReflectionMetadataBuilder.CreateMethodMetadata(
            typeof(Overloads<string>), reflected);
        await Assert.That(metadata.GetReflectionInfo()).IsSameReferenceAs(reflected);
        await Assert.That(metadata.GetReflectionInfo().GetGenericArguments()[0]).IsEqualTo(typeof(int));
    }

    [Test]
    [Arguments(1)]
    [Arguments(2)]
    public async Task Generic_array_shapes_distinguish_overloads(int rank)
    {
        var method = Create("Arrays", 0, new ArrayType(new GenericParameter(0, false), rank)).GetReflectionInfo();
        await Assert.That(method.GetParameters()[0].ParameterType.GetArrayRank()).IsEqualTo(rank);
        await Assert.That(method.GetParameters()[0].ParameterType.GetElementType()!.IsGenericParameter).IsTrue();
    }

    [Test]
    public async Task Nested_array_shapes_distinguish_overloads()
    {
        var shape = new ConstructedGeneric(typeof(List<>), [new ArrayType(new ArrayType(new GenericParameter(0, false)))]);
        var method = Create("NestedArrays", 0, shape).GetReflectionInfo();
        await Assert.That(method.GetParameters()[0].ParameterType.GetGenericArguments()[0].GetElementType()!.GetElementType()!.IsGenericParameter).IsTrue();
    }

    [Test]
    public async Task Array_metadata_compares_nested_generic_shapes_structurally()
    {
        TestMetadata CreateMetadata(int rank) => new TestMetadata<MetadataMethodLookupTests>
        {
            TestName = "Array",
            TestClassType = typeof(MetadataMethodLookupTests),
            TestMethodName = "Array",
            DataSources = [], ClassDataSources = [], PropertyDataSources = [],
            FilePath = "", LineNumber = 0, AttributeFactory = static () => [],
            MethodMetadata = Create("Array", 0,
                new ArrayType(new ConstructedGeneric(typeof(List<>), [new GenericParameter(0, false)]), rank))
        };
        var comparer = TUnit.Engine.Services.TestMetadataEqualityComparer.Instance;
        await Assert.That(comparer.Equals(CreateMetadata(1), CreateMetadata(1))).IsTrue();
        await Assert.That(comparer.Equals(CreateMetadata(1), CreateMetadata(2))).IsFalse();
    }

    private static MethodMetadata Create(string name, int arity, TUnit.Core.TypeInfo? shape = null) =>
        MethodMetadataFactory.Create(name, typeof(Overloads<>), typeof(void),
            TestContext.Current!.TestDetails.MethodMetadata.Class, arity,
            shape is null ? [] : [ParameterMetadataFactory.Create(typeof(object), "value", shape, false)]);

    public class Overloads<T>
    {
        public void Run(object value) { }
        public void Run(T value) { }
        public void Run(string value) { }
        public void Empty() { }
        public void Empty<TMethod>() { }
        public void Empty<TFirst, TSecond>() { }
        public void Nested(List<object> value) { }
        public void Nested(List<T> value) { }
        public void Array(T[] value) { }
        public void Arrays(string[] value) { }
        public void Arrays(T[,] value) { }
        public void Arrays(T[] value) { }
        public void NestedArrays(List<string[][]> value) { }
        public void NestedArrays(List<T[][]> value) { }
    }
}
