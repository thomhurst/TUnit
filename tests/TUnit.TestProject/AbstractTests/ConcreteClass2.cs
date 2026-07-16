namespace TUnit.TestProject.AbstractTests;

[InheritsTests]
public class ConcreteClass2 : ConcreteClass1
{
    protected override string GetName()
    {
        return "ConcreteClass2";
    }

    [Test]
    public void SecondTest()
    {
        // Dummy method
    }
}
