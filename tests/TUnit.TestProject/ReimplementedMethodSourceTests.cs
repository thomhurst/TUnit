using TUnit.Assertions;

namespace TUnit.TestProject;

public class ReimplementedMethodSourceTests
{
    public sealed class ExplicitSourceAttribute(string memberName) : MethodDataSourceAttribute(memberName), IDataSourceAttribute
    {
        IAsyncEnumerable<Func<Task<object?[]?>>> IDataSourceAttribute.GetDataRowsAsync(DataGeneratorMetadata metadata)
            => StringRows();
    }

    public sealed class ImplicitSourceAttribute(string memberName) : MethodDataSourceAttribute(memberName), IDataSourceAttribute
    {
        public new IAsyncEnumerable<Func<Task<object?[]?>>> GetDataRowsAsync(DataGeneratorMetadata metadata)
            => StringRows();
    }

    private static async IAsyncEnumerable<Func<Task<object?[]?>>> StringRows()
    {
        await Task.CompletedTask;
        yield return () => Task.FromResult<object?[]?>(["custom row"]);
    }

    public sealed class ConfiguredSourceAttribute(string memberName) : MethodDataSourceAttribute(memberName)
    {
        public bool UseIntegers
        {
            get => false;
            set => Arguments = value ? [1] : [];
        }
    }

    public static IEnumerable<string> OverloadedRows() => ["wrong row"];
    public static IEnumerable<int> OverloadedRows(int value) => [value];

    [Test]
    [GenerateGenericTest(typeof(int))]
    [ConfiguredSource(nameof(OverloadedRows), UseIntegers = true)]
    public async Task Named_setter_preserves_types<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(int));
        await Assert.That((object?)value).IsEqualTo(1);
    }

    public static IEnumerable<int> DecoyRows() => [42];

    [Test]
    [GenerateGenericTest(typeof(string))]
    [ExplicitSource(nameof(DecoyRows))]
    public async Task Explicit_rows_preserve_types<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That((object?)value).IsEqualTo("custom row");
    }

    [Test]
    [GenerateGenericTest(typeof(string))]
    [ImplicitSource(nameof(DecoyRows))]
    public async Task Implicit_rows_preserve_types<T>(T value)
    {
        await Assert.That(typeof(T)).IsEqualTo(typeof(string));
        await Assert.That((object?)value).IsEqualTo("custom row");
    }
}
