using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.None), NotInParallel]
public class InjectNonSharedInstance(DummyReferenceTypeClass dummyReferenceTypeClass)
{
    private static readonly List<DummyReferenceTypeClass> Instances = [];

    [Test, Repeat(5)]
    public async Task Test1()
    {
        await Assert.That(Instances).DoesNotContain(dummyReferenceTypeClass);
        Instances.Add(dummyReferenceTypeClass);
    }

    [Test, Repeat(5)]
    public async Task Test2()
    {
        await Assert.That(Instances).DoesNotContain(dummyReferenceTypeClass);
        Instances.Add(dummyReferenceTypeClass);
    }

    [Test, Repeat(5)]
    public async Task Test3()
    {
        await Assert.That(Instances).DoesNotContain(dummyReferenceTypeClass);
        Instances.Add(dummyReferenceTypeClass);
    }
}