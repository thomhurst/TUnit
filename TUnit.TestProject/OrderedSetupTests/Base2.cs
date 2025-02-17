using System.Reflection;

namespace TUnit.TestProject.OrderedSetupTests;

#pragma warning disable
public class Base2 : Base1
{
    [Before(Test)]
    public void Z_Before2()
    {
        Console.WriteLine($@"{GetType().Name}.Z_Before2");
    }

    [Before(Test)]
    public void Y_Before2()
    {
        Console.WriteLine($@"{GetType().Name}.Y_Before2");
    }

    [Before(Test)]
    public void A_Before2()
    {
        Console.WriteLine($@"{GetType().Name}.A_Before2");
    }

    [Before(Test)]
    public void B_Before2()
    {
        Console.WriteLine($@"{GetType().Name}.B_Before2");
    }

    [After(Test)]
    public void Z_After2()
    {
        Console.WriteLine($@"{GetType().Name}.Z_After2");
    }

    [After(Test)]
    public void Y_After2()
    {
        Console.WriteLine($@"{GetType().Name}.Y_After2");
    }

    [After(Test)]
    public void A_After2()
    {
        Console.WriteLine($@"{GetType().Name}.A_After2");
    }

    [After(Test)]
    public void B_After2()
    {
        Console.WriteLine($@"{GetType().Name}.B_After2");
    }
}