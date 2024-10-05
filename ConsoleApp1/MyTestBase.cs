namespace ConsoleApp1;

public abstract class MyTestBase
{
    [ClassDataSource<MyFactory>(Shared = SharedType.Globally)]
    public static MyFactory Factory { get; set; } = null!;
    public static HttpClient Client { get; set; } = null!;

    [Before(TestSession)]
    public static async Task PrepareSession()
    {
        Client = Factory.CreateClient();
        // this doesn't happen because the client breaks,
        // imagine some async initialization here
        var somethingAfterCreateClient = 1;
    }
}
