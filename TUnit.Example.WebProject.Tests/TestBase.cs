namespace TUnit.Example.WebProject.Tests;

public class TestBase
{
    [ClassDataSource<MyFactory>(Shared = SharedType.PerTestSession)]
    public required MyFactory Factory { get; set; } = null!;
    
    protected HttpClient Client { get; private set; } = null!;

    [Before(Test)]
    public void BeforeTest()
    {
        Client = Factory.CreateClient();
    }
}