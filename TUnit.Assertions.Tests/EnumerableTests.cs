using System.Collections;
using TUnit.Assertions.AssertConditions.Throws;

namespace TUnit.Assertions.Tests;

public class EnumerableTests
{
    [Test]
    public async Task Enumerable_Contains_Item_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).Contains(1);
    }
    
    [Test]
    public async Task Enumerable_Contains_Item_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
                async () => await Assert.That(array).Contains(4)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Enumerable_Contains_Matcher_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).Contains(x => x == 1);
    }
    
    [Test]
    public async Task Enumerable_Contains_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).Contains(x => x == 4)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Enumerable_ContainsOnly_Matcher_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).ContainsOnly(x => x < 10);
    }
    
    [Test]
    public async Task Enumerable_ContainsOnly_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).ContainsOnly(x => x < 3)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Enumerable_DoesNotContain_Item_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).DoesNotContain(5);
    }
    
    [Test]
    public async Task Enumerable_DoesNotContain_Item_Bad()
    {
        int[] array = [1, 2, 3];
        
        await Assert.That(
            async () => await Assert.That(array).DoesNotContain(3)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Enumerable_DoesNotContain_Matcher_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).DoesNotContain(x => x > 10);
    }
    
    [Test]
    public async Task Enumerable_DoesNotContain_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).DoesNotContain(x => x < 3)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Enumerable_Ordered_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).IsInOrder();
    }
    
    [Test]
    public async Task Enumerable_Ordered_Bad()
    {
        int[] array = [1, 3, 2];

        await Assert.That(
            async () => await Assert.That(array).IsInOrder()
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Enumerable_Ordered_Descending_Good()
    {
        int[] array = [3, 2, 1];

        await Assert.That(array).IsInDescendingOrder();
    }
    
    [Test]
    public async Task Enumerable_Ordered_Descending_Bad()
    {
        int[] array = [3, 1, 2];

        await Assert.That(
            async () => await Assert.That(array).IsInDescendingOrder()
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Untyped_Enumerable()
    {
        int[] array = [1, 2, 3];
        
        IEnumerable enumerable = array;

        await Assert.That(enumerable).IsInOrder();
    }
    
    [Test]
    public async Task Untyped_Enumerable_EqualTo()
    {
        int[] array = [1, 2, 3];
        
        IEnumerable enumerable = array;

        await Assert.That(enumerable).IsEqualTo(enumerable);
    }
    
    [Test]
    public async Task Untyped_Enumerable_ReferenceEqualTo()
    {
        int[] array = [1, 2, 3];
        
        IEnumerable enumerable = array;

        await Assert.That(enumerable).IsSameReferenceAs(enumerable);
    }
}