namespace TUnit.AspNetCore.NugetTester;

/// <summary>
/// Tests verifying that TUnit hooks and WebApplicationTest lifecycle work correctly together.
/// These tests prove that source generation and hooks function properly when TUnit.AspNetCore
/// is consumed as a NuGet package.
/// </summary>
public class HooksAndLifecycleTests : TestsBase
{
    private bool _beforeTestHookRan;
    private bool _setupAsyncRan;
    private string? _isolatedName;

    protected override async Task SetupAsync()
    {
        _setupAsyncRan = true;
        _isolatedName = GetIsolatedName("test-resource");
        await Task.CompletedTask;
    }

    [Before(HookType.Test)]
    public void BeforeTestHook()
    {
        _beforeTestHookRan = true;
        Console.WriteLine($"[BeforeTest] Starting test: {TestContext.Current?.Metadata?.DisplayName}");
    }

    [After(HookType.Test)]
    public void AfterTestHook()
    {
        // This hook runs after each test, proving After hooks work with WebApplicationTest
        Console.WriteLine($"[AfterTest] Finished test: {TestContext.Current?.Metadata?.DisplayName}");
    }

    [Test]
    public async Task Factory_IsNotNull_WhenTestRuns()
    {
        // Verifies that the Factory is properly initialized before test execution
        await Assert.That(Factory).IsNotNull();
    }

    [Test]
    public async Task Factory_CreateClient_Works()
    {
        // Verifies that we can create an HttpClient from the factory
        var client = Factory.CreateClient();
        await Assert.That(client).IsNotNull();
    }

    [Test]
    public async Task Services_AreAccessible_FromFactory()
    {
        // Verifies that Services property exposes the service provider
        await Assert.That(Services).IsNotNull();

        var greetingService = Services.GetService<IGreetingService>();
        await Assert.That(greetingService).IsNotNull();
    }

    [Test]
    public async Task GlobalFactory_IsShared_AcrossTests()
    {
        // Verifies that GlobalFactory is accessible
        await Assert.That(GlobalFactory).IsNotNull();
    }

    [Test]
    public async Task SetupAsync_RanBeforeTest()
    {
        // Verifies that SetupAsync was called before the test
        await Assert.That(_setupAsyncRan).IsTrue();
    }

    [Test]
    public async Task BeforeTestHook_RanBeforeTest()
    {
        // Verifies that the [Before(Test)] hook ran before this test
        await Assert.That(_beforeTestHookRan).IsTrue();
    }

    [Test]
    public async Task GetIsolatedName_ReturnsUniqueValue()
    {
        // Verifies that GetIsolatedName produces a unique isolation name
        await Assert.That(_isolatedName).IsNotNull();
        await Assert.That(_isolatedName).Contains("test-resource");
        await Assert.That(_isolatedName).Contains("Test_");
    }

    [Test]
    public async Task UniqueId_IsPositive()
    {
        // Verifies that UniqueId is assigned and positive
        await Assert.That(UniqueId).IsPositive();
    }

    [Test]
    public async Task GetIsolatedPrefix_ReturnsFormattedPrefix()
    {
        // Verifies that GetIsolatedPrefix produces a formatted prefix
        var prefix = GetIsolatedPrefix("_");
        await Assert.That(prefix).StartsWith("test_");
        await Assert.That(prefix).Contains("_");
    }

    [Test]
    public async Task Ping_Endpoint_ReturnsExpectedResponse()
    {
        // Basic integration test verifying the factory and web app work together
        var client = Factory.CreateClient();
        var response = await client.GetAsync("/ping");

        await Assert.That(response.IsSuccessStatusCode).IsTrue();
        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).IsEqualTo("pong");
    }

    [Test]
    [Arguments("Alice")]
    [Arguments("Bob")]
    [Arguments("Charlie")]
    public async Task DataDrivenTest_WithArguments_Works(string name)
    {
        // Verifies that data-driven tests work with WebApplicationTest
        var client = Factory.CreateClient();
        var response = await client.GetAsync($"/greet/{name}");

        var content = await response.Content.ReadAsStringAsync();
        await Assert.That(content).Contains(name);
    }
}
