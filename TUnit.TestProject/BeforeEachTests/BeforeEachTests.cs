using TUnit.Core;

namespace TUnit.TestProject.BeforeEachTests;

public class Base1
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAll1()
    {
    }
    
    [BeforeEachTest]
    public async Task BeforeEach1()
    {
    }
}

public class Base2 : Base1
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAll2()
    {
    }
    
    [BeforeEachTest]
    public async Task BeforeEach2()
    {
    }
}

public class Base3 : Base2
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAll3()
    {
    }
    
    [BeforeEachTest]
    public async Task BeforeEach3()
    {
    }
}

public class SetupTests : Base3
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAllSetUp()
    {
    }

    [AfterAllTestsInClass]
    public static async Task AfterAllTearDown()
    {
    }
    
    [BeforeEachTest]
    public async Task Setup()
    {
    }

    [Test]
    public async Task Test1()
    {
    }
    
    [Test]
    public async Task Test2()
    {
    }
}