using System.Collections.Immutable;
using TUnit.Assertions.Enums;

namespace TUnit.Assertions.Tests.Bugs;

public class Tests1917
{
    [Test]
    public async Task Immutable_Array_Has_Enumerable_Methods()
    {
        var array = ImmutableArray<string>.Empty;
        var list = ImmutableList<string>.Empty;

        await Assert.That(array).IsEmpty();
        await Assert.That(list).IsEmpty();
    }
}