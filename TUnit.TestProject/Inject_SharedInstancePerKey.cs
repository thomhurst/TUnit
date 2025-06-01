using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public static class SharedInjectedKeyedContainer
{
    public static readonly ConcurrentDictionary<string, List<DummyReferenceTypeClass>> InstancesPerKey = new();
}

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.PerClass), NotInParallel]
public class InjectSharedPerKey1(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    public static string Key => TestContext.Current!.TestDetails.TestClass.Namespace + "." + TestContext.Current.TestDetails.TestClass.Name  + "_" + TestContext.Current.TestDetails.TestName;
    
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        
        list.Add(dummyReferenceTypeClass);
        
        await Assert.That(list.Distinct()).HasSingleItem();
    }

    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        
        list.Add(dummyReferenceTypeClass);
        
        await Assert.That(list.Distinct()).HasSingleItem();
    }

    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        
        list.Add(dummyReferenceTypeClass);
        
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.PerClass), NotInParallel]
public class InjectSharedPerKey2(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    public static string Key => TestContext.Current!.TestDetails.TestClass.Namespace + "." + TestContext.Current.TestDetails.TestClass.Name  + "_" + TestContext.Current.TestDetails.TestName;

    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }

    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }

    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.PerClass), NotInParallel]
public class InjectSharedPerKey3(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    public static string Key => TestContext.Current!.TestDetails.TestClass.Namespace + "." + TestContext.Current.TestDetails.TestClass.Name  + "_" + TestContext.Current.TestDetails.TestName;

    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }

    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }

    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(Key, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }

        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != Key))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(Key, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}