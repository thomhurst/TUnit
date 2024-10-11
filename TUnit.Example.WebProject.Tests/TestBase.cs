namespace TUnit.Example.WebProject.Tests;

public class TestBase
{
    [ClassDataSource<MyFactory>(Shared = SharedType.Globally)]
    public static MyFactory Factory { get; set; } = null!;
    
    protected HttpClient Client { get; private set; } = null!;

    [Before(HookType.Test)]
    public void BeforeTest()
    {
        Client = Factory.CreateClient();
    }

    [Before(HookType.TestSession)]
    public static async Task PrepareSession()
    {
        Factory.CreateClient();
        await Task.CompletedTask;
    }
}