using System.Reflection;

namespace TUnit.Core;

public class AssemblyHookContext : Context
{
    private static readonly AsyncLocal<AssemblyHookContext?> Contexts = new();
    public new static AssemblyHookContext? Current
    {
        get => Contexts.Value;
        internal set => Contexts.Value = value;
    }
    
    internal AssemblyHookContext()
    {
    }

    public required Assembly Assembly { get; init; }

    public HashSet<ClassHookContext> TestClasses { get; init; } = [];

    public IEnumerable<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests);

    public int TestCount => AllTests.Count();
}