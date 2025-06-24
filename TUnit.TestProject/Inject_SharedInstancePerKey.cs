using System.Collections.Concurrent;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public static class SharedInjectedKeyedContainer
{
    public static readonly ConcurrentDictionary<string, List<DummyReferenceTypeClass>> InstancesPerKey = new();

    public static async Task Check(string key, DummyReferenceTypeClass dummyReferenceTypeClass)
    {
        var list = InstancesPerKey.GetOrAdd(key, _ => []);

        list.Add(dummyReferenceTypeClass);

        await Assert.That(list.Distinct()).HasSingleItem();
    }
}

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.PerClass), NotInParallel]
public class InjectSharedPerKey1(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    public static string Key
    {
        get
        {
            if (TestContext.Current is null)
            {
                throw new InvalidOperationException("TestContext.Current is null. This can happen if the test is not run in a TUnit test environment.");
            }
            return TestContext.Current!.TestDetails.ClassMetadata.Namespace + "." + TestContext.Current.TestDetails.ClassMetadata.Name + "_" + TestContext.Current.TestDetails.TestName;
        }
    }

    [Test, Repeat(5)]
    public async Task Test1()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }

    [Test, Repeat(5)]
    public async Task Test2()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }

    [Test, Repeat(5)]
    public async Task Test3()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.PerClass), NotInParallel]
public class InjectSharedPerKey2(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    public static string Key => TestContext.Current!.TestDetails.ClassMetadata.Namespace + "." + TestContext.Current.TestDetails.ClassMetadata.Name  + "_" + TestContext.Current.TestDetails.TestName;

    [Test, Repeat(5)]
    public async Task Test1()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }

    [Test, Repeat(5)]
    public async Task Test2()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }

    [Test, Repeat(5)]
    public async Task Test3()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.PerClass), NotInParallel]
public class InjectSharedPerKey3(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    public static string Key => TestContext.Current!.TestDetails.ClassMetadata.Namespace + "." + TestContext.Current.TestDetails.ClassMetadata.Name + "_"
        + TestContext.Current.TestDetails.TestName;

    [Test, Repeat(5)]
    public async Task Test1()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }

    [Test, Repeat(5)]
    public async Task Test2()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }

    [Test, Repeat(5)]
    public async Task Test3()
    {
        await SharedInjectedKeyedContainer.Check(Key, dummyReferenceTypeClass);
    }
}
