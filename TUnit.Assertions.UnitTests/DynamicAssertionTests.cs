using System.Diagnostics.CodeAnalysis;

namespace TUnit.Assertions.UnitTests;

[SuppressMessage("Trimming", "IL2026:Members annotated with \'RequiresUnreferencedCodeAttribute\' require dynamic access otherwise can break functionality when trimming application code")]
public class DynamicAssertionTests
{
    [Test]
    public async Task Test1()
    {
        dynamic? foo = null;
        await TUnitAssert.That(foo).IsNotNull();
    }
}