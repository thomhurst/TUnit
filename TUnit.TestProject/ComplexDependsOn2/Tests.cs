#pragma warning disable
namespace TUnit.TestProject.ComplexDependsOn2;

public abstract class BaseClass
{
    [Test]
    public async Task Test1()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(50));
    }

    [Test]
    [DependsOn(nameof(Test1))]
    public async Task Test2()
    {
        await Task.Delay(TimeSpan.FromMilliseconds(50));
    }
}
public class TestsDataSource
{
    public static IEnumerable<Func<string>> Data()
    {
        yield return () => "Data1";
        yield return () => "Data2";
        yield return () => "Data3";
        yield return () => "Data4";
        yield return () => "Data5";
        yield return () => "Data6";
        yield return () => "Data7";
        yield return () => "Data8";
        yield return () => "Data9";
    }
}
[InheritsTests]
[MethodDataSource<TestsDataSource>(nameof(TestsDataSource.Data))]
public class Tests(string data) : BaseClass;

[InheritsTests]
[DependsOn<Tests>]
[MethodDataSource<TestsDataSource>(nameof(TestsDataSource.Data))]
public class ReadTests(string data) : BaseClass;

[InheritsTests]
[DependsOn<ReadTests>]
[MethodDataSource<TestsDataSource>(nameof(TestsDataSource.Data))]
public class UpdateTests(string data) : BaseClass;

[InheritsTests]
[DependsOn<UpdateTests>]
[MethodDataSource<TestsDataSource>(nameof(TestsDataSource.Data))]
public class DeleteTests(string data) : BaseClass;

[InheritsTests]
[DependsOn<DeleteTests>]
[MethodDataSource<TestsDataSource>(nameof(TestsDataSource.Data))]
public class CreateTests2(string data) : BaseClass;

[InheritsTests]
[DependsOn<CreateTests2>]
[MethodDataSource<TestsDataSource>(nameof(TestsDataSource.Data))]
public class ReadTests2(string data) : BaseClass;

[InheritsTests]
[DependsOn<ReadTests2>]
[MethodDataSource<TestsDataSource>(nameof(TestsDataSource.Data))]
public class UpdateTests2(string value) : BaseClass;

[InheritsTests]
[DependsOn<UpdateTests2>]
[MethodDataSource<TestsDataSource>(nameof(TestsDataSource.Data))]
public class DeleteTest2(string value) : BaseClass;