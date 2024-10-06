﻿using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Assertions.Extensions.Generic;

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
    [MethodDataSource(nameof(SomeMethod), DisposeAfterTest = false)]
    public async Task DataSource_Method2(int value)
    {
        await Assert.That(value).IsEqualTo(1);
    }
    
    public static IEnumerable<int> SomeMethod() => [1,2,3,4,5];
}