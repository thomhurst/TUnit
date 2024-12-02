using System.Diagnostics.CodeAnalysis;
using TUnit.Assertions.Tests.Assertions.Delegates;

namespace TUnit.Assertions.Tests;

[SuppressMessage("Usage", "TUnitAssertions0005:Assert.That(...) should not be used with a constant value")]
public class AssertMultipleTests
{
    [Test]
    public async Task Exception_In_Scope_Is_Captured()
    {
        await Assert.That(async () =>
        {
            using (Assert.Multiple())
            {
                await Assert.That(1).IsEqualTo(2);
                await Assert.That(2).IsEqualTo(4);

                if (1.ToString() == "1")
                {
                    throw new Exception("Hello World");
                }

                await Assert.That(3).IsEqualTo(6);
            }
        }).Throws<Exception>().And.HasMessageContaining("Hello World");
}
}