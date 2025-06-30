using TUnit.Core;

namespace AotTest;

public class SimpleTest
{
    [Test]
    public void BasicTest()
    {
        Console.WriteLine("AOT Test Success!");
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("AOT compilation test passed!");
    }
}