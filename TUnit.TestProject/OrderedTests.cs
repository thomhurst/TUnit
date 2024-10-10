using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class OrderedTests
{
    private static readonly List<string> RegisteredOrders = [];

    [Test, NotInParallel(nameof(OrderedTests), Order = 2)]
    public async Task Second()
    {
        RegisteredOrders.Add(nameof(Second));
        await Wait();
    }
    
    [Test, NotInParallel(nameof(OrderedTests), Order = 4)]
    public async Task Fourth()
    {
        RegisteredOrders.Add(nameof(Fourth));
        await Wait();
    }
    
    [Test, NotInParallel(nameof(OrderedTests), Order = 1)]
    public async Task First()
    {
        RegisteredOrders.Add(nameof(First));
        await Wait();
    }
    
    [Test, NotInParallel(nameof(OrderedTests), Order = 5)]
    public async Task Fifth()
    {
        RegisteredOrders.Add(nameof(Fifth));
        await Wait();
    }
    
    [Test, NotInParallel(nameof(OrderedTests), Order = 3)]
    public async Task Third()
    {
        RegisteredOrders.Add(nameof(Third));
        await Wait();
    }
    
    [Test, NotInParallel(nameof(OrderedTests), Order = 6)]
    public async Task AssertOrder()
    {
        await Assert.That(RegisteredOrders)
            .IsEquivalentCollectionTo(["First", "Second", "Third", "Fourth", "Fifth"]);
    }

    private async Task Wait()
    {
        await Task.Delay(1500);
    }
}