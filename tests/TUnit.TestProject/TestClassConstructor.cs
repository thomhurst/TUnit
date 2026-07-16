using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

public class TestClassConstructor : IClassConstructor
{
    public Task<object> Create(Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        // For ClassConstructorTest, create it with a DummyReferenceTypeClass instance
        if (type == typeof(ClassConstructorTest))
        {
            return Task.FromResult<object>(new ClassConstructorTest(new DummyReferenceTypeClass()));
        }

        // For other types, use default constructor
        return Task.FromResult(Activator.CreateInstance(type)!);
    }
}

[EngineTest(ExpectedResult.Pass)]
[ClassConstructor<TestClassConstructor>]
public class SimpleClassConstructorTest
{
    [Test]
    public void Test()
    {
        // This test should pass if ClassConstructor is working
    }
}
