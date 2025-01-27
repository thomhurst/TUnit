using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject.Bugs._1692;

public class Tests
{
    private const string? NullContent = null;

    [Test]
    [Arguments(NullContent)]
    [Arguments(null)]
    public async Task NullTest(string? t) => await Assert.That(t).IsNull();
}