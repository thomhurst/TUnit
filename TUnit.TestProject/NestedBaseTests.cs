using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class NestedBaseTests : NestedBase1
{
    [Test]
    public void Test()
    {
    }

    [After(Class)]
    public static async Task AssertCounts(ClassHookContext context)
    {
        var test = context.Tests.Single();
        
        using (Assert.Multiple())
        {
            await Assert.That(test.ObjectBag[nameof(Before1)]).IsEqualTo(1);
            await Assert.That(test.ObjectBag[nameof(Before2)]).IsEqualTo(1);
            await Assert.That(test.ObjectBag[nameof(Before3)]).IsEqualTo(1);
            
            await Assert.That(test.ObjectBag[nameof(After1)]).IsEqualTo(1);
            await Assert.That(test.ObjectBag[nameof(After2)]).IsEqualTo(1);
            await Assert.That(test.ObjectBag[nameof(After3)]).IsEqualTo(1);
        }
    }
}

public class NestedBase1 : NestedBase2
{
    [Before(Test)]
    public void Before1(TestContext context)
    {
        var count = context.ObjectBag.GetValueOrDefault(nameof(Before1)) as int? ?? 0;
        context.ObjectBag[nameof(Before1)] = count + 1;
    }
    
    [After(Test)]
    public void After1(TestContext context)
    {
        var count = context.ObjectBag.GetValueOrDefault(nameof(After1)) as int? ?? 0;
        context.ObjectBag[nameof(After1)] = count + 1;
    }    
}

public class NestedBase2 : NestedBase3
{
    [Before(Test)]
    public void Before2(TestContext context)
    {
        var count = context.ObjectBag.GetValueOrDefault(nameof(Before2)) as int? ?? 0;
        context.ObjectBag[nameof(Before2)] = count + 1;
    }
    
    [After(Test)]
    public void After2(TestContext context)
    {
        var count = context.ObjectBag.GetValueOrDefault(nameof(After2)) as int? ?? 0;
        context.ObjectBag[nameof(After2)] = count + 1;
    }
}

public class NestedBase3
{
    [Before(Test)]
    public void Before3(TestContext context)
    {
        var count = context.ObjectBag.GetValueOrDefault(nameof(Before3)) as int? ?? 0;
        context.ObjectBag[nameof(Before3)] = count + 1;
    }
    
    [After(Test)]
    public void After3(TestContext context)
    {
        var count = context.ObjectBag.GetValueOrDefault(nameof(After3)) as int? ?? 0;
        context.ObjectBag[nameof(After3)] = count + 1;
    }
}