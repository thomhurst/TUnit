using System;

namespace TUnit.Assertions.Attributes;

[AttributeUsage(AttributeTargets.Method)]
public sealed class AssertionAttribute : Attribute
{
    public string Expectation { get; }
    public string? But { get; }

    public AssertionAttribute(string expectation, string? but = null)
    {
        Expectation = expectation;
        But = but;
    }
}