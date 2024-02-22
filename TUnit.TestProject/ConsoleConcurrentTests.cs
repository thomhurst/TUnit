using System.Reflection;
using TUnit.Core;

namespace TUnit.TestProject;

public class ConsoleConcurrentTests
{
    [Test, Repeat(1000)]
    public void Test1()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test2()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test3()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test4()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test5()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test6()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test7()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test8()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test9()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
    
    [Test, Repeat(1000)]
    public void Test10()
    {
        Console.WriteLine(MethodBase.GetCurrentMethod()?.Name);
    }
}