using TUnit.Assertions;
using TUnit.Core.Attributes;

namespace TUnit.TestProject;

public class Tests
{
    // [SetUp]
    // public void Setup()
    // {
    // }

    [Test]
    public void Test1()
    {
        var one = "1";
        Assert.That(one, Is.EqualTo("1"));
    }
}