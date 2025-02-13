using TUnit.Assertions.AssertConditions.Throws;
using TUnit.Assertions.Assertions.Enums;

namespace TUnit.Assertions.Tests;

public class EnumTests
{
    [Flags]
    public enum MyEnum
    {
        One = 1,
        Two = 2,
        Three = 4,
        Four = 8,
        Five = 16,
    }
    
    [Flags]
    public enum MyEnum2
    {
        One = 0,
        Two = 1,
        Three = 2,
        Four = 3,
        Five = 4,
    }
    
    [Test]
    public async Task Flags_Good()
    {
        var value = MyEnum.One | MyEnum.Two;

        await Assert.That(value)
            .HasFlag(MyEnum.One)
            .And
            .HasFlag(MyEnum.Two)
            .And
            .DoesNotHaveFlag(MyEnum.Three)
            .And
            .DoesNotHaveFlag(MyEnum.Four)
            .And
            .DoesNotHaveFlag(MyEnum.Five);
    }
    
    [Test]
    public async Task Flags_Bad()
    {
        var value = MyEnum.One | MyEnum.Two;

        await Assert.That(async () =>
            await Assert.That(value).HasFlag(MyEnum.Three)
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task IsDefined_Good()
    {
        var value = MyEnum.One;

        await Assert.That(value).IsDefined();
    }
    
    [Test]
    public async Task IsNotDefined_Good()
    {
        var value = (MyEnum) 99;

        await Assert.That(value).IsNotDefined();
    }

    [Test]
    public async Task IsDefined_Bad()
    {
        var value = MyEnum.One;

        await Assert.That(async () =>

            await Assert.That(value).IsNotDefined()
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task IsNotDefined_Bad()
    {
        var value = (MyEnum)99;

        await Assert.That(async () =>
            await Assert.That(value).IsDefined()
        ).Throws<AssertionException>();
    }

    [Test]
    public async Task HasSameNameAs_Good()
    {
        var value = MyEnum.One;
        var value2 = MyEnum2.One;

        await Assert.That(value).HasSameNameAs(value2);
    }

    [Test]
    public async Task HasSameNameAs_Bad()
    {
        var value = MyEnum.One;
        var value2 = MyEnum2.Two;

        await Assert.That(async () =>
            await Assert.That(value).HasSameNameAs(value2)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task HasSameValueAs_Good()
    {
        var value = MyEnum.One;
        var value2 = MyEnum2.Two;

        await Assert.That(value).HasSameValueAs(value2);
    }

    [Test]
    public async Task HasSameValueAs_Bad()
    {
        var value = MyEnum.One;
        var value2 = MyEnum2.One;

        await Assert.That(async () =>
            await Assert.That(value).HasSameValueAs(value2)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task DoesNotHaveSameNameAs_Good()
    {
        var value = MyEnum.One;
        var value2 = MyEnum2.Two;

        await Assert.That(value).DoesNotHaveSameNameAs(value2);
    }

    [Test]
    public async Task DoesNotHaveSameNameAs_Bad()
    {
        var value = MyEnum.One;
        var value2 = MyEnum2.One;

        await Assert.That(async () =>
            await Assert.That(value).DoesNotHaveSameNameAs(value2)
        ).Throws<AssertionException>();
    }
    
    [Test]
    public async Task DoesNotHaveSameValueAs_Good()
    {
        var value = MyEnum.One;
        var value2 = MyEnum2.One;

        await Assert.That(value).DoesNotHaveSameValueAs(value2);
    }

    [Test]
    public async Task DoesNotHaveSameValueAs_Bad()
    {
        var value = MyEnum.One;
        var value2 = MyEnum2.Two;

        await Assert.That(async () =>
            await Assert.That(value).DoesNotHaveSameValueAs(value2)
        ).Throws<AssertionException>();
    }
}