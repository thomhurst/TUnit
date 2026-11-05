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

    public class LookalikeDataSourceTests
    {
        [Lookalikes.MethodDataSource]
        public static string? Value { get; set; }

        [PropertyLookalikes.Arguments]
        public static string? OtherValue { get; set; }

        [Test]
        public async Task Custom_property_source_is_used()
        {
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
