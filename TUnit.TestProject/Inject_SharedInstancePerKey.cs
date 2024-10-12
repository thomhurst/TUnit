using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public static class SharedInjectedKeyedContainer
{
    public static readonly ConcurrentDictionary<string, List<DummyReferenceTypeClass>> InstancesPerKey = new();
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerKey1(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerKey2(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerKey3(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}