using System.Reflection;

namespace TUnit.TestProject.OrderedByAttributeOrderParameterSetupTests;
#pragma warning disable
public class Base3
{
    [Before(Test, Order = 4)]
    public void Z_Before3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test, Order = 3)]
    public void Y_Before3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test, Order = 1)]
    public void A_Before3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [Before(Test, Order = 2)]
    public void B_Before3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test, Order = 4)]
    public void Z_After3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test, Order = 3)]
    public void Y_After3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test, Order = 1)]
    public void A_After3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }

    [After(Test, Order = 2)]
    public void B_After3()
    {
        Console.WriteLine($@"{GetType().Name}.{MethodBase.GetCurrentMethod()?.Name}");
    }
}