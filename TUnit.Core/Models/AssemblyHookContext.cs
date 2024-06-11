namespace TUnit.Core.Models;

public class AssemblyHookContext
{
    internal AssemblyHookContext()
    {
    }
    
    public HashSet<ClassHookContext> TestClasses { get; } = [];
    
    public IEnumerable<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests);

    public int TestCount => AllTests.Count();
}