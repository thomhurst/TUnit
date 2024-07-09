using TUnit.Core;

namespace TUnit.TestProject.BeforeTests;

public class Base1
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAll1()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task BeforeEach1()
    {
        await Task.CompletedTask;
    }
}

public class Base2 : Base1
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAll2()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task BeforeEach2()
    {
        await Task.CompletedTask;
    }
}

public class Base3 : Base2
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAll3()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task BeforeEach3()
    {
        await Task.CompletedTask;
    }
}

public class SetupTests : Base3
{
    [BeforeAllTestsInClass]
    public static async Task BeforeAllSetUp()
    {
        await Task.CompletedTask;
    }

    [AfterAllTestsInClass]
    public static async Task AfterAllTearDown()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task Setup()
    {
        await Task.CompletedTask;
    }
    
    [BeforeEachTest]
    public async Task SetupWithContext(TestContext testContext)
    {
        await Task.CompletedTask;
    }

    [Test]
    public async Task Test1()
    {
        await Task.CompletedTask;
    }
    
    [Test]
    public async Task Test2()
    {
        await Task.CompletedTask;
    }
}