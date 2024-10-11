using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public static class SharedInjectedTypesContainer
{
    public static readonly List<DummyReferenceTypeClass> TestClass1Instances = [];
    public static readonly List<DummyReferenceTypeClass> TestClass2Instances = [];
    public static readonly List<DummyReferenceTypeClass> TestClass3Instances = [];
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerType1(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedTypesContainer.TestClass1Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Contains(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).DoesNotContain(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass1Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass1Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedTypesContainer.TestClass1Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Contains(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).DoesNotContain(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass1Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass1Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedTypesContainer.TestClass1Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).Contains(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).DoesNotContain(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass1Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass1Instances.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerType2(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedTypesContainer.TestClass2Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Contains(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).DoesNotContain(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass2Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass2Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedTypesContainer.TestClass2Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Contains(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).DoesNotContain(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass2Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass2Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedTypesContainer.TestClass2Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).Contains(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).DoesNotContain(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass2Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass2Instances.Distinct()).HasSingleItem();
    }
}

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.ForClass), NotInParallel]
public class InjectSharedPerType3(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    [Test, Repeat(5)]
    public async Task Test1()
    {
        if (SharedInjectedTypesContainer.TestClass3Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass3Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass3Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        if (SharedInjectedTypesContainer.TestClass3Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass3Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass3Instances.Distinct()).HasSingleItem();
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        if (SharedInjectedTypesContainer.TestClass3Instances.Any())
        {
            await Assert.That(SharedInjectedTypesContainer.TestClass1Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass2Instances).DoesNotContain(dummyReferenceTypeClass);
            await Assert.That(SharedInjectedTypesContainer.TestClass3Instances).Contains(dummyReferenceTypeClass);
        }

        SharedInjectedTypesContainer.TestClass3Instances.Add(dummyReferenceTypeClass);
        await Assert.That(SharedInjectedTypesContainer.TestClass3Instances.Distinct()).HasSingleItem();
    }
}