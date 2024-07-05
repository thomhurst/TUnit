using System.Collections.Concurrent;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

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
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
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
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
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
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedKeyedContainer.InstancesPerKey.TryGetValue(TestContext.Current!.TestInformation.TestName, out var list)
            && list.Any())
        {
            await Assert.That(list).Does.Contain(_dummyReferenceTypeClass);
        }
        
        foreach (var (key, value) in SharedInjectedKeyedContainer.InstancesPerKey.Where(x => x.Key != TestContext.Current!.TestInformation.TestName))
        {
            await Assert.That(list!).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        list = SharedInjectedKeyedContainer.InstancesPerKey.GetOrAdd(TestContext.Current!.TestInformation.TestName, _ => new List<DummyReferenceTypeClass>());
        list.Add(_dummyReferenceTypeClass);
        await Assert.That(list.Distinct()).Has.SingleItem();
    }
}