using System.Diagnostics.CodeAnalysis;

namespace TUnit.TestProject;

public class ExperimentalTests
{
    [Experimental("Blah")]
    [Test]
    public void SynchronousTest()
    {
        // Dummy method
    }

    [Experimental("Blah")]
    [Test]
    public async Task AsynchronousTest()
    {
        await Task.CompletedTask;
    }
}