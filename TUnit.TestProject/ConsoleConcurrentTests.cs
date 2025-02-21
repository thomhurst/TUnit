namespace TUnit.TestProject;

public class ConsoleConcurrentTests
{
    [Test, Repeat(25)]
    public void Test1()
    {
        Console.WriteLine(nameof(Test1));
    }

    [Test, Repeat(25)]
    public void Test2()
    {
        Console.WriteLine(nameof(Test2));
    }

    [Test, Repeat(25)]
    public void Test3()
    {
        Console.WriteLine(nameof(Test3));
    }

    [Test, Repeat(25)]
    public void Test4()
    {
        Console.WriteLine(nameof(Test4));
    }

    [Test, Repeat(25)]
    public void Test5()
    {
        Console.WriteLine(nameof(Test5));
    }

    [Test, Repeat(25)]
    public void Test6()
    {
        Console.WriteLine(nameof(Test6));
    }

    [Test, Repeat(25)]
    public void Test7()
    {
        Console.WriteLine(nameof(Test7));
        Console.WriteLine(nameof(Test7));
        Console.WriteLine(nameof(Test7));
        Console.WriteLine(nameof(Test7));
        Console.WriteLine(nameof(Test7));
    }

    [Test, Repeat(25)]
    public void Test8()
    {
        Console.WriteLine(nameof(Test8));
    }

    [Test, Repeat(25)]
    public void Test9()
    {
        Console.WriteLine(nameof(Test9));
    }

    [Test, Repeat(25)]
    public void Test10()
    {
        Console.WriteLine(nameof(Test10));
    }
}