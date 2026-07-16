namespace TUnit.TestProject.OrderedByAttributeOrderParameterSetupTests;
#pragma warning disable
public class Base1 : Base3
{
    [Before(Test, Order = 4)]
    public void Z_Before1()
    {
        Console.WriteLine($@"{GetType().Name}.Z_Before1");
    }

    [Before(Test, Order = 3)]
    public void Y_Before1()
    {
        Console.WriteLine($@"{GetType().Name}.Y_Before1");
    }

    [Before(Test, Order = 1)]
    public void A_Before1()
    {
        Console.WriteLine($@"{GetType().Name}.A_Before1");
    }

    [Before(Test, Order = 2)]
    public void B_Before1()
    {
        Console.WriteLine($@"{GetType().Name}.B_Before1");
    }

    [After(Test, Order = 4)]
    public void Z_After1()
    {
        Console.WriteLine($@"{GetType().Name}.Z_After1");
    }

    [After(Test, Order = 3)]
    public void Y_After1()
    {
        Console.WriteLine($@"{GetType().Name}.Y_After1");
    }

    [After(Test, Order = 1)]
    public void A_After1()
    {
        Console.WriteLine($@"{GetType().Name}.A_After1");
    }

    [After(Test, Order = 2)]
    public void B_After1()
    {
        Console.WriteLine($@"{GetType().Name}.B_After1");
    }
}
