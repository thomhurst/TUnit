namespace TUnit.TestProject.AbstractTests;

[InheritsTests]
public class ConcreteClass1 : AbstractBaseClass
{
    protected override string GetName()
    {
        return "Concrete1";
    }
}
