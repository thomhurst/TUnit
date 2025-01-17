﻿namespace TUnit.Assertions.Tests;

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

        await Assert.That(array).Contains((int x) => x == 1);
    }
    
    [Test]
    public async Task Enumerable_Contains_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).Contains((int x) => x == 4)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task Enumerable_ContainsOnly_Matcher_Good()
    {
        int[] array = [1, 2, 3];

        await Assert.That(array).ContainsOnly((int x) => x < 10);
    }
    
    [Test]
    public async Task Enumerable_ContainsOnly_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).ContainsOnly((int x) => x < 3)
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

        await Assert.That(array).DoesNotContain((int x) => x > 10);
    }
    
    [Test]
    public async Task Enumerable_DoesNotContain_Matcher_Bad()
    {
        int[] array = [1, 2, 3];

        await Assert.That(
            async () => await Assert.That(array).DoesNotContain((int x) => x < 3)
        ).Throws<AssertionException>();
    }
}