using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions.Strings;

namespace TUnit.TestProject.AbstractTests;

public abstract class AbstractBaseClass
{
    [Test]
    public async Task AssertClassName()
    {
        var name = GetName();

        await Assert.That(name).IsEqualTo(GetType().Name, StringComparison.Ordinal);
    }

    protected abstract string GetName();
}