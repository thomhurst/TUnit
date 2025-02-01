﻿using TUnit.Assertions;
using TUnit.Assertions.Extensions;
using TUnit.Core.Extensions;

namespace TUnit.TestProject.Bugs._1570;

[Arguments(1)]
[Arguments(2)]
[Arguments(3)]
public class Tests(int number)
{
    [Test]
    public async Task Dependency()
    {
        await Task.Delay(TimeSpan.FromSeconds(number));

        TestContext.Current!.ObjectBag["number"] = number;
    }

    [Test]
    [DependsOn(nameof(Dependency))]
    public async Task GetTests_Without_Filtering_On_TestClassArguments_Test()
    {
        var dependencyContext = TestContext.Current!
            .GetTests(nameof(Dependency))
            .First();

        await Assert.That(dependencyContext.ObjectBag["number"]).IsEqualTo(number);
    }
}