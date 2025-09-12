using System;
using TUnit.Core;

namespace TUnit.TestProject.Bugs._2971;

[AttributeUsage(AttributeTargets.Assembly)]
public class SomeAttribute : Attribute
{
    public SomeAttribute(Type type)
    {
        Type = type;
    }
    
    public Type Type { get; }
}

public class Tests
{
    [Test]
    public void SimpleTest()
    {
        // Empty test to trigger source generation
    }
}