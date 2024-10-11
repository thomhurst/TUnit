using System.Text;

namespace TUnit.Assertions.Tests;

public class TypeOfTests
{
    [Test]
    public async Task Returns_Casted_Object()
    {
        object? obj = new StringBuilder();
        
        var result = await Assert.That(obj).IsTypeOf<StringBuilder>();

        await Assert.That(result).IsNotNull();
    }
}