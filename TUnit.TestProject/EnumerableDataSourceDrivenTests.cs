﻿using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace TUnit.TestProject;

public class EnumerableDataSourceDrivenTests
{
    [Test]
    [MethodDataSource(nameof(SomeMethod))]
    public async Task DataSource_Method(int value)
    {
        await Assert.That(value).IsEqualTo(1);
    }
    
    [Test]
    [MethodDataSource(nameof(SomeMethod))]
    public async Task DataSource_Method2(int value)
    {
        await Assert.That(value).IsEqualTo(1);
    }
    
    [Test]
    [MethodDataSource(nameof(MethodWithBaseReturn))]
    public void DataSource_WithBaseReturn(BaseValue value)
    {
    }
    
    public static IEnumerable<int> SomeMethod() => [1,2,3,4,5];

    public static List<Func<BaseValue>> MethodWithBaseReturn() =>
    [
        () => new ConcreteValue(),
        () => new ConcreteValue2()
    ];
    
    public abstract class BaseValue;

    public class ConcreteValue : BaseValue;
    public class ConcreteValue2 : BaseValue;
}