namespace TUnit.TestProject.AfterTests;

public class Base1
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

public class Base2 : Base1
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

public class Base3 : Base2
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

public class CleanupTests : Base3
{
    [After(Class)]
    public static async Task AfterAllCleanUp()
    {
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AfterAllCleanUpWithContext(ClassHookContext context)
    {
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AfterAllCleanUp(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [After(Class)]
    public static async Task AfterAllCleanUpWithContext(ClassHookContext context, CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task Cleanup()
    {
        await Task.CompletedTask;
    }

    [After(Test), Timeout(30_000)]
    public async Task Cleanup(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }

    [After(Test)]
    public async Task CleanupWithContext(TestContext testContext)
    {
        await Task.CompletedTask;
    }

    [After(Test), Timeout(30_000)]
    public async Task CleanupWithContext(TestContext testContext, CancellationToken cancellationToken)
    {
        await Assertions.Extensions.GenericIsNotExtensions.IsNotNull(Assertions.Assert.That(testContext.Result));
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
