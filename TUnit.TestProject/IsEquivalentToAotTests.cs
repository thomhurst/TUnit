using System.Collections.Generic;
using TUnit.Assertions.Extensions;
using TUnit.Core;

namespace TUnit.TestProject;

public class IsEquivalentToAotTests
{
    [Test]
    public async Task IsEquivalentTo_WithCustomComparer_WorksWithAot()
    {
        List<int> list1 = [1, 2, 3];
        List<int> list2 = [2, 3, 1];

        // This should NOT require unreferenced code when using a custom comparer
        await Assert.That(list1).IsEquivalentTo(list2, comparer: EqualityComparer<int>.Default);
    }

    [Test]
    public async Task IsEquivalentTo_WithoutComparer_RequiresUnreferencedCode()
    {
        List<int> list1 = [1, 2, 3];
        List<int> list2 = [2, 3, 1];

        // This WILL require unreferenced code (structural comparison)
        await Assert.That(list1).IsEquivalentTo(list2);
    }
}
