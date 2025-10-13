#if NET6_0_OR_GREATER
using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.Tests;

public class IndexAssertionTests
{
    [Test]
    public async Task Test_Index_IsFromEnd()
    {
        Index index = ^1; // Last element
        await Assert.That(index).IsFromEnd();
    }

    [Test]
    public async Task Test_Index_IsFromEnd_Explicit()
    {
        var index = Index.FromEnd(5);
        await Assert.That(index).IsFromEnd();
    }

    [Test]
    public async Task Test_Index_IsNotFromEnd()
    {
        Index index = 0; // First element
        await Assert.That(index).IsNotFromEnd();
    }

    [Test]
    public async Task Test_Index_IsNotFromEnd_Explicit()
    {
        var index = Index.FromStart(10);
        await Assert.That(index).IsNotFromEnd();
    }
}
#endif
