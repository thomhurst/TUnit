namespace TUnit.Core.Models;

public class AssemblyHookContext
{
    public List<ClassHookContext> TestClasses { get; } = [];
    
    public IEnumerable<TestContext> AllTests => TestClasses.SelectMany(x => x.Tests);

    public int TestCount => AllTests.Count();
}