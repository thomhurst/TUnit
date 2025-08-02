namespace TUnit.TestProject;

// Base class with test methods
public class BaseTestClass
{
    [Test]
    public void BaseTest()
    {
        Console.WriteLine("Running base test");
    }
}

// Derived class with InheritsTests
[InheritsTests]
public class DerivedTestClass : BaseTestClass { }
