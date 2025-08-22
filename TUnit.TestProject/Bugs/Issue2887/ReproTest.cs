using System.Diagnostics.CodeAnalysis;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject.Bugs.Issue2887;

public interface IServiceProvider;

public sealed class DependencyInjectionClassConstructor : IClassConstructor
{
    public Task<object> Create([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        throw new NotImplementedException();
    }
}

public abstract class BaseTestClass(IServiceProvider serviceProvider)
{
    [Before(Test)]
    public Task Hook() => Task.CompletedTask;
}

[ClassConstructor<DependencyInjectionClassConstructor>]
public sealed class ActualTestClass(IServiceProvider serviceProvider)
    : BaseTestClass(serviceProvider)
{
    [Test]
    public void Test1() { }
}