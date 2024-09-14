using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public static class SharedInjectedKeyedContainer
{
    public static readonly ConcurrentDictionary<string, List<DummyReferenceTypeClass>> InstancesPerKey = new();
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerKey1
{
    private readonly DummyReferenceTypeClass _dummyReferenceTypeClass;
    
    public InjectSharedPerKey1(DummyReferenceTypeClass dummyReferenceTypeClass)
    {
        _dummyReferenceTypeClass = dummyReferenceTypeClass;
    }

    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerKey2
{
    private readonly DummyReferenceTypeClass _dummyReferenceTypeClass;
    
    public InjectSharedPerKey2(DummyReferenceTypeClass dummyReferenceTypeClass)
    {
        _dummyReferenceTypeClass = dummyReferenceTypeClass;
    }

    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerKey3
{
    private readonly DummyReferenceTypeClass _dummyReferenceTypeClass;
    
    public InjectSharedPerKey3(DummyReferenceTypeClass dummyReferenceTypeClass)
    {
        _dummyReferenceTypeClass = dummyReferenceTypeClass;
    }

    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestDetails.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Contains(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestDetails.TestName))
        {
            await Assert.That(list!).DoesNotContain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestDetails.TestName, _ =>
            []);
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).HasSingleItem();
    }
}