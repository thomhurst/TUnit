namespace TUnit.TestProject.Bugs._7445;

[Arguments("seed")]
[MethodDataSource(nameof(Rows))]
public class SyncNullCollection<T>(T value)
{
    public static IEnumerable<string>? Rows() => null;

    [Test]
    public async Task Seed_Is_The_Only_Row() => await Assert.That((object?)value).IsEqualTo("seed");
}

[Arguments("seed")]
[MethodDataSource(nameof(Rows))]
public class TaskNullCollection<T>(T value)
{
    public static Task<IEnumerable<string>?> Rows() => System.Threading.Tasks.Task.FromResult<IEnumerable<string>?>(null);

    [Test]
    public async Task Seed_Is_The_Only_Row() => await Assert.That((object?)value).IsEqualTo("seed");
}

[Arguments("seed")]
[MethodDataSource(nameof(Rows))]
public class ValueTaskNullCollection<T>(T value)
{
    public static ValueTask<IEnumerable<string>?> Rows() => System.Threading.Tasks.ValueTask.FromResult<IEnumerable<string>?>(null);

    [Test]
    public async Task Seed_Is_The_Only_Row() => await Assert.That((object?)value).IsEqualTo("seed");
}

[Arguments("seed")]
[MethodDataSource(nameof(Rows))]
public class AsyncNullCollection<T>(T value)
{
    public static IAsyncEnumerable<string>? Rows() => null;

    [Test]
    public async Task Seed_Is_The_Only_Row() => await Assert.That((object?)value).IsEqualTo("seed");
}

[GenerateGenericTest(typeof(string))]
[MethodDataSource(nameof(Rows))]
public class NullElement<T>(T value)
{
    public static IEnumerable<string?> Rows() => [null];

    [Test]
    public async Task Null_Row_Is_Retained() => await Assert.That((object?)value).IsNull();
}
