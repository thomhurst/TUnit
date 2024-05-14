using TUnit.Core;

namespace TUnit.TestProject;

public class CleanUpBase1
{
    [AfterAllTestsInClass]
    public static async Task AfterAll1()
    {
    }
    
    [AfterEachTest]
    public async Task AfterEach1()
    {
    }
}

public class CleanUpBase2 : CleanUpBase1
{
    [AfterAllTestsInClass]
    public static async Task AfterAll2()
    {
    }
    
    [AfterEachTest]
    public async Task AfterEach2()
    {
    }
}

public class CleanUpBase3 : CleanUpBase2
{
    [AfterAllTestsInClass]
    public static async Task AfterAll3()
    {
    }
    
    [AfterEachTest]
    public async Task AfterEach3()
    {
    }
}

public class CleanUpTests : CleanUpBase3, IDisposable
{
    [AfterAllTestsInClass]
    public static async Task FinalClean()
    {
    }
    
    [AfterEachTest]
    public async Task CleanUp()
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

    public void Dispose()
    {
    }
}