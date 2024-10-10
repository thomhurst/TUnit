using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public static class SharedInjectedGloballyContainer
{
    public static readonly List<DummyReferenceTypeClass> Instances = [];
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.Globally), NotInParallel]
public class InjectSharedGlobally1(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.Globally), NotInParallel]
public class InjectSharedGlobally2(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.Globally), NotInParallel]
public class InjectSharedGlobally3(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedGloballyContainer.Instances.Any())
        {
            await Assert.That(SharedInjectedGloballyContainer.Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedGloballyContainer.Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedGloballyContainer.Instances.Distinct()).HasSingleItem();
    }
}