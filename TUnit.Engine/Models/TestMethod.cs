using System.Reflection;

namespace TUnit.Engine.Models;

internal class TestMethod
{
    public required Type TestClass { get; init; }
    public required MethodInfo MethodInfo { get; init; }
}