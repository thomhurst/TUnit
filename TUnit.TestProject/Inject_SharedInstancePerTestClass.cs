using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public static class SharedInjectedTypesContainer
{
    public static readonly List<DummyReferenceTypeClass> TestClass1Instances = new();
    public static readonly List<DummyReferenceTypeClass> TestClass2Instances = new();
    public static readonly List<DummyReferenceTypeClass> TestClass3Instances = new();
}

[Inject<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class Inject_SharedPerType1
{
    private readonly DummyReferenceTypeClass _dummyReferenceTypeClass;
    
    public Inject_SharedPerType1(DummyReferenceTypeClass dummyReferenceTypeClass)
    {
        _dummyReferenceTypeClass = dummyReferenceTypeClass;
    }

    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedTypesContainer.TestClass1Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass1Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass1Instances.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedTypesContainer.TestClass1Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass1Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass1Instances.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedTypesContainer.TestClass1Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass1Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass1Instances.Distinct()).Has.SingleItem();
    }
}

[Inject<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class Inject_SharedPerType2
{
    private readonly DummyReferenceTypeClass _dummyReferenceTypeClass;
    
    public Inject_SharedPerType2(DummyReferenceTypeClass dummyReferenceTypeClass)
    {
        _dummyReferenceTypeClass = dummyReferenceTypeClass;
    }

    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedTypesContainer.TestClass2Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass2Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass2Instances.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedTypesContainer.TestClass2Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass2Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass2Instances.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedTypesContainer.TestClass2Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass2Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass2Instances.Distinct()).Has.SingleItem();
    }
}

[Inject<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class Inject_SharedPerType3
{
    private readonly DummyReferenceTypeClass _dummyReferenceTypeClass;
    
    public Inject_SharedPerType3(DummyReferenceTypeClass dummyReferenceTypeClass)
    {
        _dummyReferenceTypeClass = dummyReferenceTypeClass;
    }

    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedTypesContainer.TestClass3Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass3Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass3Instances.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedTypesContainer.TestClass3Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass3Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass3Instances.Distinct()).Has.SingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedTypesContainer.TestClass3Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Does.Not.Contain(_dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Does.Contain(_dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass3Instances.Add(_dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass3Instances.Distinct()).Has.SingleItem();
    }
}