using TUnit.Assertions.Extensions;

namespace TUnit.Assertions.UnitTests;

public class ObjectTests
{
    [Test]
    public async Task Assertion_Message_Has_Correct_Expression()
    {
        var myModel = new MyModel("1", "2");
        var myModel2 = new MyModel("1", "2");

        await Assert.That(myModel).IsEqualTo(myModel2);
    }
    
    public record MyModel(string One, string Two);
}