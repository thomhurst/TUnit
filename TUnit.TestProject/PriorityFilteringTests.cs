using TUnit.Core;
using TUnit.Core.Enums;

namespace TUnit.TestProject;

public class PriorityFilteringTests
{
    [Test, Priority(PriorityLevel.High)]
    public async Task High_1()
    {
        await Task.CompletedTask;
    }
    
    [Test, Priority(PriorityLevel.High)]
    public async Task High_2()
    {
        await Task.CompletedTask;
    }
    
    [Test, Priority(PriorityLevel.High)]
    public async Task High_3()
    {
        await Task.CompletedTask;
    }
    
    [Test, Priority(PriorityLevel.Medium)]
    public async Task Medium_1()
    {
        await Task.CompletedTask;
    }
    
    [Test, Priority(PriorityLevel.Medium)]
    public async Task Medium_2()
    {
        await Task.CompletedTask;
    }
    
    [Test, Priority(PriorityLevel.Low)]
    public async Task Low_1()
    {
        await Task.CompletedTask;
    }
}