using System.Reflection;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

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

    public HashSet<ClassHookContext> TestClasses { get; } = [];

    public IEnumerable<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests);

    public int TestCount => AllTests.Count();
}