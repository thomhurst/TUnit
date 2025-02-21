namespace TUnit.TestProject;

public class CleanUpBase1
{
    [After(Class)]
    public static async Task AfterAll1()
    {
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task AfterEach1()
    {
        await Task.CompletedTask;
    }
}

public class CleanUpBase2 : CleanUpBase1
{
    [After(Class)]
    public static async Task AfterAll2()
    {
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task AfterEach2()
    {
        await Task.CompletedTask;
    }
}

public class CleanUpBase3 : CleanUpBase2
{
    [After(Class)]
    public static async Task AfterAll3()
    {
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task AfterEach3()
    {
        await Task.CompletedTask;
    }
}

public class CleanUpTests : CleanUpBase3, IDisposable
{
    [After(Class)]
    public static async Task FinalClean()
    {
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task CleanUp()
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

    public void Dispose()
    {
        // Dummy method
    }
}