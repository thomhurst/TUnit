using System.Reflection;

namespace TUnit.TestProject.OrderedSetupTests;

#pragma warning disable
public class Base3
{
    [Before(Test)]
    public void Z_Before3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test)]
    public void Y_Before3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test)]
    public void A_Before3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test)]
    public void B_Before3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test)]
    public void Z_After3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test)]
    public void Y_After3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test)]
    public void A_After3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test)]
    public void B_After3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }
}