namespace TUnit.DocTests;

public sealed class DocumentationWebApplicationFactory : TestWebApplicationFactory<Program>
{
}

public abstract class SnippetContext : WebApplicationTest<DocumentationWebApplicationFactory, Program>
{
    protected static CancellationToken cancellationToken => default;
    protected static CancellationToken ct => default;
}

public sealed class Program
{
}

public interface IEntity<out TId>
{
    TId Id { get; }
}

public interface IValidatable
{
    bool IsValid();
}

public sealed record User(string Name = "Alice") : IEntity<Guid>
{
    public object Id { get; init; } = 0;
    public string Role { get; init; } = "User";
    public string Email { get; init; } = "alice@example.com";
    public int Age { get; init; } = 30;
    public string FirstName { get; init; } = "Alice";
    public string LastName { get; init; } = "Example";
    public string[] Permissions { get; init; } = ["read"];
    public string[] Roles { get; init; } = ["Admin"];
    public bool IsActive { get; init; } = true;
    public DateTime CreatedDate { get; init; } = DateTime.UtcNow;

    public bool HasPermission(string permission) => Permissions.Contains(permission);
    public bool CanRead => true;
    public bool CanWrite => true;
    public bool CanDelete => false;

    Guid IEntity<Guid>.Id => Id is Guid id ? id : Guid.Empty;
}

public sealed class Calculator
{
    public int Add(int left, int right) => left + right;
    public int Multiply(int left, int right) => left * right;
}

public class AppFixture : AspireFixture<Projects.MyAppHost>
{
}

public sealed class ApiClientFixture
{
    public HttpClient Client { get; } = new();
}

public sealed class RedisFixture
{
    public StackExchange.Redis.IDatabase Database => throw new NotImplementedException();
}

public sealed class InMemoryDatabase : ITestDatabase
{
    public PostgreSqlContainer Container => throw new NotImplementedException();
    public void Initialize() { }
    public Task InitializeAsync() => Task.CompletedTask;
    public void Dispose() { }
}

public class WebApplicationFactory : TestWebApplicationFactory<Program>
{
    public DatabaseConnection Database { get; } = new();
}

public class WebAppFactory : TestWebApplicationFactory<Program>
{
}

public class SharedFactory : TestWebApplicationFactory<Program>
{
}

public class EfCoreWebApplicationFactory : TestWebApplicationFactory<Program>
{
}

public abstract class TestsBase : WebApplicationTest<WebApplicationFactory, Program>
{
}

public sealed record Todo(int Id = 1, string Title = "Example");

public sealed class Counter : Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenElement(0, "p");
        builder.AddContent(1, "Current count: 0");
        builder.CloseElement();
    }
}

public sealed class MyRequest
{
    public string Payload { get; init; } = string.Empty;
    public string TestId { get; init; } = string.Empty;
}

public sealed record OrderMessage;

public static class Projects
{
    public class MyAppHost : global::Aspire.Hosting.IProjectMetadata
    {
        public string ProjectPath => "MyAppHost.csproj";
        public global::Aspire.Hosting.LaunchSettings? LaunchSettings => null;
        public bool IsFileBasedApp => false;
        public bool SuppressBuild => true;
    }

    public sealed class AppHostA : MyAppHost
    {
    }

    public sealed class AppHostB : MyAppHost
    {
    }

    public sealed class MyApp_AppHost : MyAppHost
    {
    }
}

public sealed record Person(string Name = "Alice", int Age = 42);
