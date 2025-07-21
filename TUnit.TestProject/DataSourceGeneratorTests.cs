﻿
using TUnit.TestProject.Attributes;

#pragma warning disable CS9113 // Parameter is unread.
namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
[AutoFixtureGenerator<int, string, bool>]
[AutoFixtureGenerator]
public class DataSourceGeneratorTests(int value, string value2, bool value3)
{
    [Test]
    [AutoFixtureGenerator<int>]
    public void GeneratedData_Method(int value)
    {
    }

    [Test]
    [AutoFixtureGenerator<int, string, bool>]
    public void GeneratedData_Method2(int value, string value2, bool value3)
    {
    }

    [Test]
    [AutoFixtureGenerator]
    public void GeneratedData_Method3(int value, string value2, bool value3)
    {
    }
}
