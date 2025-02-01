using System.Reflection;

namespace TUnit.TestProject.OrderedSetupTests;

#pragma warning disable
public class Base1 : Base3
{
    [Before(Test)]
    public void Z_Before1()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test)]
    public void Y_Before1()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test)]
    public void A_Before1()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test)]
    public void B_Before1()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test)]
    public void Z_After1()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test)]
    public void Y_After1()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test)]
    public void A_After1()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test)]
    public void B_After1()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }
}