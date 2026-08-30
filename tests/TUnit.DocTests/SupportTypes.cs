using TUnit.Core.Interfaces;

namespace TUnit.DocTests
{
    public static class DocValues
    {
        public static int actual => 1;
        public static int expected => 1;
        public static int value => 1;
        public static int actualValue => 1;
        public static int expectedValue => 1;
        public static int age => 30;
        public static int score => 100;
        public static double temperature => 21.5;
        public static bool condition => true;
        public static DateTime futureDate => DateTime.UtcNow.AddDays(1);
        public static bool isValid => true;
        public static int[] collection => [1, 2, 3];
        public static int item => 1;
        public static string text => "example";
        public static string pattern => "example";
        public static string prefix => "ex";
        public static string suffix => "ple";
        public static string substring => "amp";
        public static string email => "alice@example.com";
        public static string filename => "example.txt";
        public static string input => "example";
        public static string message => "example";
        public static string username => "alice";
        public static int[] items => [1, 2, 3];
        public static int[] list => [1, 2, 3];
        public static int[] numbers => [1, 2, 3];
        public static int[] otherNumbers => [3, 2, 1];
        public static object[] objects => [new(), new()];
        public static int[] values => [1, 2, 3];
        public static Task longRunningTask => Task.CompletedTask;
        public static object obj => new();
        public static object? optional => null;
        public static Order order => new();
        public static object other => new();
        public static Person person => new();
        public static int result => 1;
        public static HttpStatusCode statusCode => HttpStatusCode.OK;
        public static User user => user1;
        public static User[] users => GetUsers();
        public static Product[] products => [new Product()];
        public static object someObject => new();
        public static object obj1 => new();
        public static object obj2 => obj1;
        public static object obj3 => new();
        public static int value1 => 1;
        public static int value2 => 2;
        public static HttpClient _httpClient => new();
        public static HttpClient _client => new();
        public static AppFixture fixture => new();
        public static WebApplicationBuilder builder => WebApplication.CreateBuilder();
        public static DatabaseConnection Database => new();
        public static DatabaseConnection _connection => new();
        public static string[] _queries => ["SELECT 1"];
        public static IContainer _container => new ContainerBuilder("alpine:3.23").Build();
        public static WebApplicationFactory _factory => new();
        public static HttpClient Client => new();
        public static HttpResponseMessage response => new(HttpStatusCode.OK);
        public static ExampleFactory Factory => new();
        public static TestContext context => TestContext.Current!;
        public static TestContext testContext => TestContext.Current!;
        public static CancellationToken externalToken => default;
        public static string ConnectionString => "Server=localhost";
        public static string connectionString => "Host=localhost;Database=examples;Username=postgres;Password=postgres";
        public static IServiceCollection services => new ServiceCollection();
        public static Microsoft.Extensions.Configuration.IConfiguration Configuration =>
            new ConfigurationBuilder().AddInMemoryCollection().Build();
        public static ExampleRepository _repository => new();
        public static ConcurrentDictionary<int, bool> _cache => new();
        public static IPage _page => null!;
        public static IBrowserContext _browserContext => null!;
        public static TestWebApplicationFactory<Program> myExistingFactory => null!;
        public static object? _value;
        public static object? _response;
        public static IDisposable _resource = new Connection();
        public static AsyncLocal<string> _myAsyncLocal = new();
        public static User validUser => user;
        public static string[] results => ["result"];
        public static Order invalidOrder => new();
        public static Mock<IUniversalService> mock => Mock.Of<IUniversalService>();
        public static Mock<IUniversalService> mockLogger => mock;
        public static Mock<IUniversalService> mockRepo => mock;
        public static IUniversalService svc => mock.Object;
        public static MockHttpClient client => Mock.HttpClient("https://example.com");
        public static MockHttpHandler handler => client.Handler;
        public static MockLogger logger => Mock.Logger();
        public static MyService myService => new();
        public static string requestId => "request-1";
        public static Func<object, TestContext, ValueTask> eventHandler => (_, _) => ValueTask.CompletedTask;
        public static User user1 => new("Alice");
        public static User user2 => new("Bob");
        public static User user3 => new("Charlie");
        public static User alice => user1;
        public static User bob => user2;
        public static User charlie => user3;
        public static User[] expected1 => [user1];
        public static User[] expected2 => [user2];
        public static ExampleDatabase database => new();
        public static ExampleApi _api => new();
        public static ExampleApi Api => new();
        public static MessagePump messagePump => new();

        public static void DoSomething() { }
        public static Task DoSomethingAsync() => Task.CompletedTask;
        public static void DivideByZero() => throw new DivideByZeroException();
        public static Task<string> FetchDataAsync() => Task.FromResult("data");
        public static int Add(int left, int right) => left + right;
        public static Animal GetAnimal() => new Dog();
        public static T GetService<T>() where T : class => (T)(object)new ExampleUserService();
        public static Type LoadPluginType() => typeof(MyService);
        public static Task<Animal> GetAnimalAsync() => Task.FromResult<Animal>(new Dog());
        public static Dog GetDog() => new();
        public static Task<User> GetUserAsync() => Task.FromResult(user);
        public static Task<User> GetUserAsync(int id) => Task.FromResult(user);
        public static Task<User> GetUserAsync(string id) => Task.FromResult(user);
        public static Task<Product> GetProductAsync() => Task.FromResult(new Product());
        public static Task<Product> GetProductAsync(int id) => Task.FromResult(new Product());
        public static ExampleConfiguration LoadConfiguration() => new();
        public static ExampleConfiguration LoadConfiguration(string environment) => new();
        public static TestResult GetCurrentResult() => TestContext.Current!.Execution.Result!;
        public static string GetInput() => "input";
        public static bool GetOptionalFlag() => true;
        public static Task<object> PerformOperation() => Task.FromResult<object>(new());
        public static Task PerformOperationAsync() => Task.CompletedTask;
        public static Task LongRunningOperationAsync() => Task.CompletedTask;
        public static Task LongRunningOperationAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public static Task FailingOperationAsync() => Task.FromException(new InvalidOperationException());
        public static Task SuccessfulOperationAsync() => Task.CompletedTask;
        public static void SafeOperation() { }
        public static int ProcessValue(int input) => input;
        public static bool ValidateInput(object? input) => input is not null;
        public static bool ValidateEmail(string address) => true;
        public static bool ValidateUser(User candidate) => true;
        public static bool EmailValidator(string address) => true;
        public static bool Calculate(int input) => true;
        public static int Calculate(int left, int right) => left + right;
        public static bool SomeLogic() => true;
        public static bool SomeLogic(int first, int second) => first == second;
        public static bool SomeLogic(int value, string text) => value > 0 && text.Length > 0;
        public static void ThrowsException() => throw new ArgumentException("example", "paramName");
        public static Task ThrowsExceptionAsync() => Task.FromException(new InvalidOperationException());
        public static Task InsertDuplicateAsync() => Task.FromException(new DbUpdateException("duplicate key"));
        public static IServiceProvider ConfigureServices() => new ServiceCollection().BuildServiceProvider();
        public static void ProcessData() { }
        public static void ProcessData(object? data) { }
        public static Task ProcessDataAsync() => Task.CompletedTask;
        public static Task ProcessDataAsync(User value) => Task.CompletedTask;
        public static Task ProcessDataAsync(object value) => Task.CompletedTask;
        public static Task ProcessDataInBackgroundAsync() => Task.CompletedTask;
        public static void ProcessInvalidData() => throw new ValidationException();
        public static void ProcessInvalidData(User value) => throw new ValidationException();
        public static void ProcessInvalidData(object? value) => throw new ArgumentNullException("data");
        public static void RiskyOperation() => throw new InvalidOperationException();
        public static Task RiskyOperationAsync() => Task.FromException(new InvalidOperationException());
        public static bool PerformCheck() => true;
        public static bool CheckIfExpired() => false;
        public static bool CheckIfExpired(DateTime value) => value < DateTime.UtcNow;
        public static bool IsNormallyDistributed(IEnumerable<double>? measurements) => measurements is not null;
        public static Task ParallelOperationAsync() => Task.CompletedTask;
        public static Task RetryAsync() => Task.CompletedTask;
        public static async Task<string> RetryAsync(Func<Task<string>> action, int maxRetries) => await action();
        public static Task ConsumeItemsAsync() => Task.CompletedTask;
        public static async Task ConsumeItemsAsync(ChannelReader<int> reader)
        {
            await foreach (var value in reader.ReadAllAsync()) { _ = value; }
        }
        public static async Task ProduceItemsAsync(ChannelWriter<int> writer)
        {
            await writer.WriteAsync(1);
            writer.Complete();
        }
        public static async IAsyncEnumerable<int> ProduceItemsAsync()
        {
            yield return 1;
            await Task.CompletedTask;
        }
        public static Task<string[]> LoadTestCasesFromDatabaseAsync() => Task.FromResult(new[] { "case" });
        public static Task<User> LoadUserAsync() => Task.FromResult(user);
        public static Task<User> LoadUserAsync(int id) => Task.FromResult(user);
        public static Task<User[]> LoadFromDatabaseAsync() => Task.FromResult(users);
        public static Task<User[]> LoadFromDatabaseAsync(IEnumerable<int> ids) => Task.FromResult(users);
        public static Task<bool> LoadFromDatabaseAsync(int id) => Task.FromResult(true);
        public static Task<Team> GetTeamAsync() => Task.FromResult(new Team());
        public static Task<Order> GetOrderAsync() => Task.FromResult(order);
        public static Task<Order[]> GetOrdersAsync() => Task.FromResult(new[] { order });
        public static Task<string> GetDataAsync() => Task.FromResult("data");
        public static Task<string> GetValueAsync() => Task.FromResult("value");
        public static string GetValue() => "value";
        public static string GetUsername() => username;
        public static string GetRequiredValue() => "value";
        public static string? GetOptionalString() => null;
        public static int? GetOptionalValue() => null;
        public static bool GetFlag() => true;
        public static bool GetFeatureFlag() => true;
        public static Task<bool?> GetFeatureFlag(string name) => Task.FromResult<bool?>(true);
        public static User GetCurrentUser() => user;
        public static int GetCount() => 1;
        public static Task<Company> GetCompanyAsync() => Task.FromResult(new Company());
        public static Task<Customer> GetCustomerAsync() => Task.FromResult(new Customer());
        public static Task<double[]> GetMeasurementsAsync() => Task.FromResult(new[] { 1.0 });
        public static Dictionary<string, object> GetFileMetadata() => new()
        {
            ["ContentType"] = "text/plain", ["Size"] = 1L, ["LastModified"] = DateTime.UtcNow
        };
        public static Dictionary<string, string> GetConfigurationValues() => new()
        {
            ["DatabaseConnection"] = "localhost", ["ApiKey"] = "key", ["Environment"] = "Test", ["Database"] = "examples"
        };
        public static Task<string> GetAuthTokenAsync() => Task.FromResult("token");
        public static Task<int> CalculateAsync() => Task.FromResult(1);
        public static Task<int> CalculateAsync(int left, int right) => Task.FromResult(left + right);
        public static int CalculateResult() => 1;
        public static double CalculateStandardDeviation(IEnumerable<double> values) => 0;
        public static int ComputeValue() => 1;
        public static Task<string> SimulateAsyncWork() => Task.FromResult("complete");
        public static Task SomeLongRunningOperation() => Task.CompletedTask;
        public static Task SomeLongRunningOperation(CancellationToken cancellationToken) => Task.CompletedTask;
        public static Task NotifyTestFinished() => Task.CompletedTask;
        public static Task LogMetrics() => Task.CompletedTask;
        public static Task LogMetrics(TestContext context) => Task.CompletedTask;
        public static Task CaptureScreenshot() => Task.CompletedTask;
        public static Task CaptureScreenshot(string path) => Task.CompletedTask;
        public static Task CaptureScreenshot(CancellationToken cancellationToken) => Task.CompletedTask;
        public static Task SaveScreenshot(string name) => Task.CompletedTask;
        public static Task<bool> IsServiceAvailable() => Task.FromResult(true);
        public static Task ProcessRequest() => Task.CompletedTask;
        public static Task ProcessContentAsync() => Task.CompletedTask;
        public static Task ProcessContentAsync(string content) => Task.CompletedTask;
        public static void ProcessContent() { }
        public static void ProcessContent(string content) { }
        public static Task ProcessOrder() => Task.CompletedTask;
        public static Task ProcessOrder(OrderMessage message) => Task.CompletedTask;
        public static Task DoWork(MyRequest request) => Task.CompletedTask;
        public static Task SaveUserAsync() => Task.CompletedTask;
        public static Task SaveUserAsync(User value) => Task.CompletedTask;
        public static Task SaveUsersBatchAsync() => Task.CompletedTask;
        public static Task SaveUsersBatchAsync(IEnumerable<User> values) => Task.CompletedTask;
        public static Task SeedTestDataAsync() => Task.CompletedTask;
        public static Task SeedTestDataAsync(string connectionString) => Task.CompletedTask;
        public static Task RunMigrationsAsync() => Task.CompletedTask;
        public static Task RunMigrationsAsync(string connectionString) => Task.CompletedTask;
        public static Task CreateTableAsync(string name) => Task.CompletedTask;
        public static Task CreateQueueAsync(string name) => Task.CompletedTask;
        public static Task CreateSchemaAsync(string name) => Task.CompletedTask;
        public static Task CreateSchemaAsync() => Task.CompletedTask;
        public static Task CleanupTestDataAsync() => Task.CompletedTask;
        public static Task<string> QueryDatabase() => Task.FromResult("database.log");
        public static Task<string> ExecuteHttpRequests() => Task.FromResult("http.log");
        public static Task<string> CollectTraces() => Task.FromResult("traces.log");
        public static Task BackgroundLoopAsync() => Task.CompletedTask;
        public static string CollectEnvironmentInfo() => "environment";
        public static void StartLogging() { }
        public static void StartLogging(string path) { }
        public static Task<string> StopRecording() => Task.FromResult("recording.webm");
        public static void GenerateReport() { }
        public static void GenerateReport(string path) { }
        public static bool CheckDatabaseConnection() => true;
        public static bool CheckExternalService() => true;
        public static Task<bool> CheckApiAvailability() => Task.FromResult(true);
        public static Task CallApi() => Task.CompletedTask;
        public static User[] GetSortedList1() => [alice, bob];
        public static User[] GetSortedList2() => [bob, charlie];
        public static User[] GetUsers() => [alice, bob, charlie];
        public static Task<User[]> GetAllUsersFromDatabase() => Task.FromResult(users);
        public static Task<int> GetUserCountFromDatabase() => Task.FromResult(users.Length);
        public static Task<ExampleTransaction> BeginTransactionAsync() => Task.FromResult(new ExampleTransaction());
        public static Task<User> CreateUserAsync(string name) => Task.FromResult(new User(name));
        public static Task<ExampleRecord> CreateRecordAsync() => Task.FromResult(new ExampleRecord(DateTime.UtcNow));
        public static Person? FindPerson(string id) => null;
        public static User CreateUser() => user;
        public static DependencyService CreateService() => new();
        public static ExampleToken CreateToken() => new(DateTime.UtcNow.AddHours(1));
        public static ExampleToken CreateExpiredToken() => new(DateTime.UtcNow.AddHours(-1));
        public static Task<string> AddToBag() => Task.FromResult("item-1");
        public static Task DeleteFromBag(object itemId) => Task.CompletedTask;
    }

    public interface IUniversalService
    {
        event Action<object?, string>? OnMessage;
        string Name { get; set; }
        int Count { get; set; }
        int Add(int left, int right);
        int Compute(int one, int two, int three, int four, int five);
        void Delete(int id);
        string Format(string value);
        User[] GetByRole(string role);
        string GetConfig();
        bool GetRole(string role);
        User GetUser(int id);
        Task<User> GetUserAsync(int id);
        string GetValue(string key);
        string Greet(string name);
        void Log(params object[] values);
        int Multiply(int left, int right);
        string Process(string value);
        string Process(int value);
        bool ProcessItems(List<int> values);
        string[] Search(string query);
        string[] Search(string query, int limit);
        bool SendEmail(string address, string subject, string body);
        void SendMessage(string message);
        Task SaveAsync(object value);
        bool SetAge(int age);
        void SetState(string state);
        int Sum(params int[] values);
        void Swap(ref int left, ref int right);
        bool TryGet(string key, out string value);
    }

    public sealed class Sut
    {
        private readonly IHttpClientFactory _factory;

        public Sut(IHttpClientFactory factory) => _factory = factory;

        public Task DoWork()
        {
            _ = _factory.CreateClient();
            return Task.CompletedTask;
        }
    }

    public abstract class Animal { }
    public sealed class Dog : Animal { }
    public sealed class MyClass { }
    public sealed record ExampleToken(DateTime ExpiresAt);
    public sealed record ExampleRecord(DateTime CreatedAt);
    public sealed record ChargeResult(bool Success);
    public sealed record Cart(decimal Total);
    public interface IPaymentGateway
    {
        Task<ChargeResult> ChargeAsync(decimal amount);
    }
    public sealed class CheckoutService
    {
        private readonly IPaymentGateway? _paymentGateway;

        public CheckoutService() { }
        public CheckoutService(IPaymentGateway paymentGateway) => _paymentGateway = paymentGateway;

        public Task<decimal> ApplyDiscountAsync(Order order, string code) => Task.FromResult(order.Total);
        public Task<double> ApplyDiscountAsync(string tier, double subtotal) =>
            Task.FromResult(tier == "GOLD" ? subtotal * 0.8 : subtotal * 0.9);
        public Task<ChargeResult> CompleteAsync(Cart cart) =>
            _paymentGateway?.ChargeAsync(cart.Total) ?? Task.FromResult(new ChargeResult(false));
    }
    public sealed class AuthConfig { public string Token { get; init; } = string.Empty; }
    public interface ITestHelper { }
    public sealed class TestHelper : ITestHelper { }
    public sealed record ProductResponse(int Id = 1, string Name = "Widget");
    public interface ITestDataSeeder { Task SeedAsync(); }
    public sealed class Cat : Animal { }
    public readonly record struct Point(int X, int Y);
    public sealed record Circle(double Radius = 1);
    public interface IMovable { }
    public interface IService
    {
        event Action<object?, string>? OnMessage;
        User GetUser(int id);
        string GetName();
        int GetCount();
    }
    public interface IMyService : IService { }
    public interface IUserService
    {
        User GetUser(int id);
        User[] GetByRole(string role);
        User[] Search(string name, int page);
    }
    public sealed class ExampleUserService : IUserService
    {
        public User GetUser(int id) => DocValues.user;
        public User[] GetByRole(string role) => DocValues.users;
        public User[] Search(string name, int page) => DocValues.users;
    }
    public sealed class ExampleFactory { public object Create(string name) => new UserService(new DatabaseConnection()); }
    public static class DatabaseQuery { public static User[] GetAllUsers() => DocValues.users; }
    public class ProductionService
    {
        public virtual TestConfig GetConfig() => new();
        public virtual void DoWork() { }
    }
    public sealed class TestConfig { }
    public static class TestDatabase
    {
        public static Task SeedAsync() => Task.CompletedTask;
        public static Task ResetAsync() => Task.CompletedTask;
        public static Task CloseConnectionsAsync() => Task.CompletedTask;
    }
    public sealed record ServiceCallResult(string Status);
    public static class FlakyService
    {
        public static Task<ServiceCallResult> CallAsync() => Task.FromResult(new ServiceCallResult("OK"));
        public static Task<ServiceCallResult> CallAsync(CancellationToken cancellationToken) =>
            Task.FromResult(new ServiceCallResult("OK"));
    }
    public interface IGreeter
    {
        string Greet(string name);
    }
    public interface IConnection
    {
        event EventHandler? OnMessage;
    }
    public interface IInvocationFeatures
    {
        T? Get<T>();
    }
    public interface IFunctionBindingsFeature
    {
        object? InvocationResult { get; }
    }
    public interface IEntity
    {
        string Name { get; set; }
        int Count { get; set; }
        bool TryGet(string key, out string value);
        void Swap(ref int value);
    }
    public class Entity : IEntity
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public int Count { get; set; }
        public bool TryGet(string key, out string value) { value = "found-value"; return true; }
        public void Swap(ref int value) => value = 99;
    }
    public class BaseRepository { }
    public sealed class DataTransferObject { }
    public sealed class AsyncResource : IAsyncDisposable
    {
        public bool IsDisposed { get; private set; }
        public ValueTask DisposeAsync() { IsDisposed = true; return ValueTask.CompletedTask; }
    }
    public sealed class Connection : IDisposable
    {
        public void SendData(string data) => throw new InvalidOperationException("not connected");
        public void Dispose() { }
    }
    public sealed class Container : IDisposable
    {
        public void Dispose() { }
    }
    public sealed class ExpensiveCalculator
    {
        public int? CachedResult { get; private set; }
        public int GetResult() => CachedResult ??= 42;
    }
    public sealed class RateLimiter(int maxRequests, TimeSpan perTimeSpan)
    {
        public int MaxRequests { get; } = maxRequests;
        public TimeSpan PerTimeSpan { get; } = perTimeSpan;
        public Task ExecuteAsync(Func<Task> action) => action();
    }
    public sealed class CircuitBreaker
    {
        public Task ExecuteAsync(Func<Task> action) => action();
    }
    public sealed class CircuitBreakerOpenException : Exception { }
    public sealed class Workflow
    {
        public WorkflowState CurrentState { get; private set; }
        public Task StartAsync() { CurrentState = WorkflowState.Started; return Task.CompletedTask; }
    }
    public enum WorkflowState { NotStarted, Started }
    public sealed class Service { public Service() { } public Service(object dependency) { } }
    public class MyService
    {
        public MyService() { }
        public MyService(string connectionString, int timeout) { }
        public MyService(Microsoft.Extensions.Logging.ILogger logger) { }
        public MyService(Microsoft.Extensions.Logging.ILogger<MyService> logger) { }
        public void Start() { }
        public Task ProcessAsync(string id) => Task.CompletedTask;
        public Task SendAsync(MyRequest request) => Task.CompletedTask;
        public DatabaseConnection? Connection { get; private set; }
        public Task InitializeAsync() { Connection = new DatabaseConnection(); return Task.CompletedTask; }
    }
    public sealed class ExampleBackgroundService
    {
        public bool IsRunning { get; private set; }
        public Task StartAsync() { IsRunning = true; return Task.CompletedTask; }
    }
    public sealed class ExampleTransaction : IDisposable
    {
        public Task RollbackAsync() => Task.CompletedTask;
        public void Dispose() { }
    }
    public static class CacheService
    {
        public static Task WarmUpAsync() => Task.CompletedTask;
        public static bool IsWarmedUp => true;
        public static bool IsWarmed => true;
    }
    public static class FeatureFlags
    {
        public static Task LoadAsync() => Task.CompletedTask;
        public static Task ResetAllAsync() => Task.CompletedTask;
        public static Task<int> CountAsync() => Task.FromResult(0);
        public static bool IsEnabled(string name) => true;
    }
    public static class ReportingService
    {
        public static ValueTask ReportTestStarted(params object?[] values) => ValueTask.CompletedTask;
        public static ValueTask ReportTestCompleted(params object?[] values) => ValueTask.CompletedTask;
    }
    public static class TelemetryClient
    {
        public static void TrackMetric(string name, double value) { }
    }
    public enum ProductStatus { Pending, Active, Preview, Inactive }
    public enum OrderStatus { Pending, Completed }

    public sealed class ExampleDatabase
    {
        public Task<User[]> GetActiveUsersAsync() => Task.FromResult(DocValues.GetUsers());
        public Task<User[]> GetUsersSortedByNameAsync() => Task.FromResult(DocValues.GetUsers());
    }

    public sealed class ExampleApi
    {
        public Task<User[]> GetUsersAsync() => Task.FromResult(DocValues.GetUsers());
        public Task<PingResult> PingAsync() => Task.FromResult(new PingResult(true));
    }
    public sealed record PingResult(bool IsSuccess);
    public sealed record PaymentResult(bool Success);
    public static class OrderRepository
    {
        public static Task<Order> CreateAsync(string item) => Task.FromResult(new Order());
    }
    public static class PaymentApi
    {
        public static Task<PaymentResult> ChargeAsync(decimal amount) => Task.FromResult(new PaymentResult(true));
    }
    public sealed class MessagePump
    {
        public event Action? ShuttingDown;
        public Task RunAsync(CancellationToken cancellationToken) { ShuttingDown?.Invoke(); return Task.CompletedTask; }
    }
    public sealed class MyCustomProcessor : BaseProcessor<Activity>
    {
        public override void OnEnd(Activity data) { }
    }

    public sealed class ExampleConfiguration
    {
        public bool IsValid => true;
        public DatabaseSettings DatabaseConnection { get; } = new();
        public bool EnableAdvancedFeatures => false;
        public bool EnableNewFeature => true;
        public bool EnableBetaFeature => false;
        public object? AdvancedSettings => null;
    }

    public sealed class DatabaseSettings
    {
        public string Server { get; init; } = "localhost";
        public string Database { get; init; } = "examples";
    }

    public sealed class Address
    {
        public string Street { get; init; } = "1 Main Street";
        public string City { get; init; } = "Seattle";
        public string ZipCode { get; init; } = "98101";
    }

    public sealed class Team
    {
        public string Name { get; init; } = "Team Alpha";
        public User[] Members { get; init; } = DocValues.users;
        public DateTime CreatedDate { get; init; } = DateTime.UtcNow;
    }

    public sealed class Company
    {
        public string Name { get; init; } = "TechCorp";
        public Address Address { get; init; } = new();
        public User[] Employees { get; init; } = DocValues.users;
    }

    public sealed class ExampleRepository
    {
        public Task<object?> FindByIdAsync(string id) => Task.FromResult<object?>(id == "valid-id" ? new() : null);
    }

    public sealed class DependencyService
    {
        public object Logger { get; } = new();
        public object Repository { get; } = new();
        public object Cache { get; } = new();
    }

    public sealed class WindowsOnlyAttribute() : SkipAttribute("Windows only")
    {
        public override Task<bool> ShouldSkip(TestRegisteredContext context) => Task.FromResult(false);
    }

    public sealed class AssignTestIdentifiersAttribute : Attribute, ITestDiscoveryEventReceiver
    {
        public ValueTask OnTestDiscovered(DiscoveredTestContext context) => ValueTask.CompletedTask;
    }

    public sealed class MyParallelLimit : IParallelLimit
    {
        public int Limit => Environment.ProcessorCount;
    }

    public sealed class TimingTestExecutor : ITestExecutor
    {
        public ValueTask ExecuteTest(TestContext context, Func<ValueTask> action) => action();
    }

    public sealed class LoggingHookExecutor : IHookExecutor
    {
        public ValueTask ExecuteBeforeTestDiscoveryHook(MethodMetadata method, BeforeTestDiscoveryContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteBeforeTestSessionHook(MethodMetadata method, TestSessionContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteBeforeAssemblyHook(MethodMetadata method, AssemblyHookContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteBeforeClassHook(MethodMetadata method, ClassHookContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteBeforeTestHook(MethodMetadata method, TestContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterTestDiscoveryHook(MethodMetadata method, TestDiscoveryContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterTestSessionHook(MethodMetadata method, TestSessionContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterAssemblyHook(MethodMetadata method, AssemblyHookContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterClassHook(MethodMetadata method, ClassHookContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterTestHook(MethodMetadata method, TestContext context, Func<ValueTask> action) => action();
    }

    public sealed class MyCustomExecutor : IHookExecutor
    {
        public ValueTask ExecuteBeforeTestDiscoveryHook(MethodMetadata method, BeforeTestDiscoveryContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteBeforeTestSessionHook(MethodMetadata method, TestSessionContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteBeforeAssemblyHook(MethodMetadata method, AssemblyHookContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteBeforeClassHook(MethodMetadata method, ClassHookContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteBeforeTestHook(MethodMetadata method, TestContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterTestDiscoveryHook(MethodMetadata method, TestDiscoveryContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterTestSessionHook(MethodMetadata method, TestSessionContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterAssemblyHook(MethodMetadata method, AssemblyHookContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterClassHook(MethodMetadata method, ClassHookContext context, Func<ValueTask> action) => action();
        public ValueTask ExecuteAfterTestHook(MethodMetadata method, TestContext context, Func<ValueTask> action) => action();
    }

    public sealed class MyFormatter { }

    public sealed class FileLogSink(string path) : TUnit.Core.Logging.ILogSink
    {
        public string Path { get; } = path;
        public bool IsEnabled(TUnit.Core.Logging.LogLevel level) => true;
        public void Log(TUnit.Core.Logging.LogLevel level, string message, Exception? exception, Context? context) { }
        public ValueTask LogAsync(TUnit.Core.Logging.LogLevel level, string message, Exception? exception, Context? context) => ValueTask.CompletedTask;
    }

    public sealed class DebugLogSink : TUnit.Core.Logging.ILogSink
    {
        public bool IsEnabled(TUnit.Core.Logging.LogLevel level) => true;
        public void Log(TUnit.Core.Logging.LogLevel level, string message, Exception? exception, Context? context) { }
        public ValueTask LogAsync(TUnit.Core.Logging.LogLevel level, string message, Exception? exception, Context? context) => ValueTask.CompletedTask;
    }

    public sealed class DatabaseConnection : IDisposable
    {
        public ExampleContainer Container { get; } = new();
        public static Task<DatabaseConnection> CreateAsync() => Task.FromResult(new DatabaseConnection());
        public void Open() { }
        public Task OpenAsync() => Task.CompletedTask;
        public void Close() { }
        public Task MigrateAsync() => Task.CompletedTask;
        public Task ExecuteAsync(string command) => Task.CompletedTask;
        public Task SeedAsync() => Task.CompletedTask;
        public Task<int> GetUserCountAsync() => Task.FromResult(1);
        public Task<int> CountAsync() => Task.FromResult(1);
        public Task<User> CreateUserAsync(string name) => Task.FromResult(new User(name));
        public Task CloseAsync() => Task.CompletedTask;
        public IEnumerable<User> Query(string query) => DocValues.users;
        public void Dispose() { }
    }

    public sealed class ExampleContainer
    {
        public string GetConnectionString() => "Host=localhost;Database=examples";
    }

    public sealed class DatabaseFixture
    {
        public DatabaseConnection Connection { get; } = new();
        public Task<object> QueryAsync(string query) => Task.FromResult<object>(new());
    }

    public sealed class SomeClass1 { }
    public sealed class SomeClass2 { }
    public sealed class SomeClass3 { }
    public sealed class Class1 { }
    public sealed class Class2 { }
    public sealed class Class3 { }
    public sealed class SomeDependency { }

    public sealed class SomeClass
    {
        public int One { get; init; }
        public int Two { get; init; }
    }

    public sealed class Customer { public Address Address { get; init; } = new(); }
    public sealed class Order
    {
        public Guid Id { get; init; } = Guid.NewGuid();
        public int OrderId { get; init; } = 1;
        public OrderStatus Status { get; init; } = OrderStatus.Pending;
        public int ProductId { get; init; }
        public string ProductName { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public decimal Total { get; init; }
        public DateTime? CompletedDate { get; init; } = DateTime.UtcNow;
        public OrderItem[] Items { get; init; } = [new("ABC-123")];
        public Customer Customer { get; init; } = new();
    }
    public sealed record OrderItem(string Sku);

    public sealed class Product
    {
        public string Name { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public string Category { get; init; } = "General";
        public ProductStatus Status { get; init; } = ProductStatus.Active;
        public int Stock { get; init; } = 1;
        public bool BackorderAllowed { get; init; }
        public object? Warranty { get; init; }
        public string? ISBN { get; init; }
    }

    public sealed class DatabaseContext { }

    public interface ITestDatabase : IDisposable
    {
        void Initialize();
        Task InitializeAsync();
    }

    public interface IOrderService : IDisposable
    {
        Order CreateOrder(int productId, string productName, decimal price);
    }

    public sealed class OrderService : IOrderService
    {
        public OrderService(ITestDatabase database) { }
        public OrderService(Microsoft.Extensions.Logging.ILogger<OrderService> logger) { }

        public Order CreateOrder(int productId, string productName, decimal price) =>
            new() { ProductId = productId, ProductName = productName, Price = price };

        public void ProcessOrder(Order value) { }

        public void Dispose() { }
        public static Task<OrderService> CreateAsync() => Task.FromResult(new OrderService(new InMemoryDatabase()));
        public static Task<Order> CreateAsync(string item) => Task.FromResult(new Order());
    }

    public sealed class ProductService : IDisposable
    {
        public ProductService(ITestDatabase database) { }

        public Product CreateProduct(string name, decimal price) => new() { Name = name, Price = price };
        public Product? GetProduct(int id) => null;
        public void Dispose() { }
    }

    public interface IUserRepository { }
    public sealed class UserRepository : IUserRepository
    {
        public UserRepository() { }
        public UserRepository(DatabaseConnection connection) { }
        public User GetUser(int id) => new() { Id = id };
        public IEnumerable<User> GetAllUsers() => DocValues.users;
        public static Task<User> GetByIdAsync(Guid id) => Task.FromResult(DocValues.user);
        public static Task<User> CreateAsync(User value) => Task.FromResult(value);
        public static Task<User> CreateAsync(string name) => Task.FromResult(new User(name));
        public static Task DeleteAsync(object id) => Task.CompletedTask;
        public static Task<bool> ExistsAsync(object id) => Task.FromResult(false);
    }

    public interface IEmailService { }
    public sealed class FakeEmailService : IEmailService
    {
        public int SentCount { get; private set; }
    }

    public sealed class UserService : IService
    {
        public event Action<object?, string>? OnMessage;
        public Microsoft.Extensions.Logging.ILogger? Logger { get; }
        public UserService(DatabaseConnection connection) { }
        public UserService(IUserRepository repository, IEmailService emailService) { }
        public UserService(Microsoft.Extensions.Logging.ILogger logger) => Logger = logger;
        public User GetUser(int id) => DocValues.user;
        public string GetName() => "Alice";
        public int GetCount() => 1;
        public Task InitializeAsync() => Task.CompletedTask;
        public Task CreateAsync(string email) => Task.CompletedTask;
        public Task<User> CreateUserAsync(string email, string name) => Task.FromResult(new User(name) { Email = email });
        public Task<User> GetUserAsync(int id) => throw new UserNotFoundException();
        public void RaiseMessage(string message) => OnMessage?.Invoke(this, message);
    }

    public sealed class UserNotFoundException : Exception { }

    public sealed class ApplicationDbContext : DbContext
    {
        public DbSet<User> Users => Set<User>();
    }
}

namespace MyCompany.Testing
{
    public abstract class DatabaseTestBase { }
}
