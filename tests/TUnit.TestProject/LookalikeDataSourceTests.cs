using TUnit.Assertions;

namespace TUnit.TestProject.Lookalikes
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class ArgumentsAttribute : Attribute
    {
        public ArgumentsAttribute(params object[] values) { }
    }

    public sealed class MethodDataSourceAttribute : DataSourceGeneratorAttribute<string>
    {
        protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            yield return () => "custom method source";
        }
    }
}

namespace TUnit.TestProject.DerivedSources
{
    public sealed class MethodDataSourceAttribute(string memberName) : TUnit.Core.MethodDataSourceAttribute(memberName);
    public sealed class InstanceSourceAttribute(string memberName) : TUnit.Core.InstanceMethodDataSourceAttribute(memberName);
    public sealed class ClassAliasAttribute(string label) : TUnit.Core.MethodDataSourceAttribute(typeof(LookalikeDataSourceTests), nameof(LookalikeDataSourceTests.RealRows));
    public sealed class AliasAttribute(string label) : TUnit.Core.MethodDataSourceAttribute(nameof(LookalikeDataSourceTests.RealRows));
}

namespace TUnit.TestProject.PropertyLookalikes
{
    public sealed class ArgumentsAttribute : DataSourceGeneratorAttribute<string>
    {
        protected override IEnumerable<Func<string>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            yield return () => "custom arguments source";
        }
    }
}

namespace TUnit.TestProject
{
    [Lookalikes.Arguments("unrelated class row")]
    [Arguments(42)]
    public class LookalikeClassArgumentsTests<T>(T value)
    {
        [Test]
        [Lookalikes.Arguments("unrelated method row")]
        public async Task Only_real_class_row_is_used()
        {
            await Assert.That(typeof(T)).IsEqualTo(typeof(int));
            await Assert.That((object?)value).IsEqualTo(42);
        }
    }

    [GenerateGenericTest(typeof(string))]
    public class RemappedClassSourceTests<T>
    {
        public static IEnumerable<int> DecoyRows() => [99];

        [Test]
        [DerivedSources.ClassAlias(nameof(DecoyRows))]
        public async Task Remapped_source_preserves_class_types(T value)
        {
            await Assert.That(typeof(T)).IsEqualTo(typeof(string));
            await Assert.That((object?)value).IsEqualTo("real row");
        }
    }

    public class LookalikeDataSourceTests
    {
        [Lookalikes.MethodDataSource]
        public static string? Value { get; set; }

        [PropertyLookalikes.Arguments]
        public static string? OtherValue { get; set; }

        [DerivedSources.MethodDataSource(nameof(DerivedValue))]
        public static string? DerivedProperty { get; set; }

        public static string DerivedValue() => "derived method source";

        public IEnumerable<int> InstanceRows() => [42];
        public static IEnumerable<object> ObjectRows() => [42];
        public static IEnumerable<int> DecoyRows() => [99];
        public static IEnumerable<string> RealRows() => ["real row"];

        [Test]
        [GenerateGenericTest(typeof(object))]
        [DerivedSources.InstanceSource(nameof(InstanceRows))]
        [MethodDataSource(nameof(ObjectRows))]
        public async Task Derived_instance_source_preserves_explicit_types<T>(T value)
        {
            await Assert.That(typeof(T)).IsEqualTo(typeof(object));
            await Assert.That((object?)value).IsEqualTo(42);
        }

        [Test]
        [GenerateGenericTest(typeof(string))]
        [DerivedSources.Alias(nameof(DecoyRows))]
        public async Task Remapped_source_uses_real_rows<T>(T value)
        {
            await Assert.That(typeof(T)).IsEqualTo(typeof(string));
            await Assert.That((object?)value).IsEqualTo("real row");
        }

        [Test]
        [DerivedSources.MethodDataSource(nameof(DerivedValue))]
        public async Task Derived_method_source_is_inferred<T>(T value)
        {
            await Assert.That(typeof(T)).IsEqualTo(typeof(string));
            await Assert.That((object?)value).IsEqualTo("derived method source");
        }

        [Test]
        public async Task Custom_property_source_is_used()
        {
            await Assert.That(DerivedProperty).IsEqualTo("derived method source");
            await Assert.That(Value).IsEqualTo("custom method source");
            await Assert.That(OtherValue).IsEqualTo("custom arguments source");
        }

        [Test]
        [Lookalikes.Arguments("unrelated method row")]
        [Arguments(42)]
        public async Task Only_real_method_row_is_used<T>(T value)
        {
            await Assert.That(typeof(T)).IsEqualTo(typeof(int));
            await Assert.That((object?)value).IsEqualTo(42);
        }
    }
}
