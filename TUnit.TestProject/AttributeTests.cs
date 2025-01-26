// ReSharper disable All
#pragma warning disable
namespace TUnit.TestProject;

public class AttributeTests
{
    [Test]
    [Mixed]
    [Mixed("Foo")]
    [Mixed("Foo", "Bar")]
    [Mixed(property2: "Foo")]
    [Mixed(Property = "Foo")]
    [Mixed("Foo", Property = "Bar")]
    [Mixed(property2: "Foo", Property = "Bar")]
    [Mixed(Property = "Foo", Property2 = 1)]
    public void MyTest()
    {
    }
    
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class MixedAttribute(string? property = null, string? property2 = null) : Attribute
    {
        public string? Property { get; set; }
        public int Property2 { get; set; }
    }
}