using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

[ClassDataSource<DummyReferenceTypeClass>(Shared = SharedType.None), NotInParallel]
public class Inject_NonSharedInstance
{
    private readonly DummyReferenceTypeClass _dummyReferenceTypeClass;
    
    private static readonly List<DummyReferenceTypeClass> Instances = new();
    
    public Inject_NonSharedInstance(DummyReferenceTypeClass dummyReferenceTypeClass)
    {
        _dummyReferenceTypeClass = dummyReferenceTypeClass;
    }

    [Test, Repeat(5)]
    public async Task Test1()
    {
        await Assert.That(Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        Instances.Add(_dummyReferenceTypeClass);
    }
    
    [Test, Repeat(5)]
    public async Task Test2()
    {
        await Assert.That(Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        Instances.Add(_dummyReferenceTypeClass);
    }
    
    [Test, Repeat(5)]
    public async Task Test3()
    {
        await Assert.That(Instances).Does.Not.Contain(_dummyReferenceTypeClass);
        Instances.Add(_dummyReferenceTypeClass);
    }
}