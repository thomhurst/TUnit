using TUnit.Assertions;
using TUnit.Core;

namespace TUnit.TestProject;

public abstract class BaseClass
{
    [Test]
    public async Task AssertClassName()
    {
        var name = GetName();

        await Assert.That(name).Is.EqualTo(GetType().Name, StringComparison.Ordinal);
    }

    protected abstract string GetName();
}

public class ConcreteClass1 : BaseClass
{
    protected override string GetName()
    {
        return "Concrete1";
    }
}

public class ConcreteClass2 : BaseClass
{
    protected override string GetName()
    {
        return "ConcreteClass2";
    }
}