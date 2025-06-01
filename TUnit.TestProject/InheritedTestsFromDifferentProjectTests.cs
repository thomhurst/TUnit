﻿using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class InheritedTestsFromDifferentProjectTests : Library.BaseTests
{
    [Test]
    public void Test()
    {
    }

    [Test]
    [MethodDataSource<TestData>(nameof(TestData.Foo))]
    public void GenericMethodDataSource(string value)
    {
    }

    [Test]
    [MethodDataSource(typeof(TestData), nameof(TestData.Foo))]
    public void NonGenericMethodDataSource(string value)
    {
    }
}