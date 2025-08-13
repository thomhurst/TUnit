using System.Diagnostics;
using TUnit.TestProject.Attributes;
using TUnit.Core.Interfaces;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class ParallelPropertyInjectionTests
{
    // Simulated container classes that take time to initialize
    public class RedisContainer : IAsyncInitializer
    {
        public DateTime InitializedAt { get; private set; }
        public TimeSpan InitializationDuration { get; private set; }
        
        public async Task InitializeAsync()
        {
            var startTime = DateTime.UtcNow;
            // Simulate Redis container startup
            await Task.Delay(100);
            InitializedAt = DateTime.UtcNow;
            InitializationDuration = InitializedAt - startTime;
            Console.WriteLine($"Redis initialized at {InitializedAt:HH:mm:ss.fff}, took {InitializationDuration.TotalMilliseconds}ms");
        }
    }
    
    public class SqlContainer : IAsyncInitializer
    {
        public DateTime InitializedAt { get; private set; }
        public TimeSpan InitializationDuration { get; private set; }
        
        public async Task InitializeAsync()
        {
            var startTime = DateTime.UtcNow;
            // Simulate SQL container startup
            await Task.Delay(150);
            InitializedAt = DateTime.UtcNow;
            InitializationDuration = InitializedAt - startTime;
            Console.WriteLine($"SQL initialized at {InitializedAt:HH:mm:ss.fff}, took {InitializationDuration.TotalMilliseconds}ms");
        }
    }
    
    public class MessageBusContainer : IAsyncInitializer
    {
        public DateTime InitializedAt { get; private set; }
        public TimeSpan InitializationDuration { get; private set; }
        
        public async Task InitializeAsync()
        {
            var startTime = DateTime.UtcNow;
            // Simulate message bus container startup
            await Task.Delay(120);
            InitializedAt = DateTime.UtcNow;
            InitializationDuration = InitializedAt - startTime;
            Console.WriteLine($"MessageBus initialized at {InitializedAt:HH:mm:ss.fff}, took {InitializationDuration.TotalMilliseconds}ms");
        }
    }
    
    // Web application factory with multiple injected containers
    public class WebApplicationFactory
    {
        [MethodDataSource<WebApplicationFactory>(nameof(GetRedisContainer))]
        public required RedisContainer Redis { get; set; }
        
        [MethodDataSource<WebApplicationFactory>(nameof(GetSqlContainer))]
        public required SqlContainer Sql { get; set; }
        
        [MethodDataSource<WebApplicationFactory>(nameof(GetMessageBusContainer))]
        public required MessageBusContainer MessageBus { get; set; }
        
        public static Func<RedisContainer> GetRedisContainer()
        {
            return () => new RedisContainer();
        }
        
        public static Func<SqlContainer> GetSqlContainer()
        {
            return () => new SqlContainer();
        }
        
        public static Func<MessageBusContainer> GetMessageBusContainer()
        {
            return () => new MessageBusContainer();
        }
    }
    
    [Test]
    [ClassDataSource<WebApplicationFactory>]
    public async Task Test_ParallelPropertyInitialization_ShouldInitializeContainersInParallel(WebApplicationFactory factory)
    {
        // Verify all containers are initialized
        await Assert.That(factory.Redis).IsNotNull();
        await Assert.That(factory.Sql).IsNotNull();
        await Assert.That(factory.MessageBus).IsNotNull();
        
        // Check that they were all initialized (IAsyncInitializer.InitializeAsync was called)
        await Assert.That(factory.Redis.InitializedAt).IsNotEqualTo(default(DateTime));
        await Assert.That(factory.Sql.InitializedAt).IsNotEqualTo(default(DateTime));
        await Assert.That(factory.MessageBus.InitializedAt).IsNotEqualTo(default(DateTime));
        
        // Calculate the total time if they were sequential vs parallel
        var totalSequentialTime = factory.Redis.InitializationDuration + 
                                  factory.Sql.InitializationDuration + 
                                  factory.MessageBus.InitializationDuration;
        
        // Find the actual total time (should be close to the max individual time if parallel)
        var earliestStart = new[] { 
            factory.Redis.InitializedAt - factory.Redis.InitializationDuration,
            factory.Sql.InitializedAt - factory.Sql.InitializationDuration,
            factory.MessageBus.InitializedAt - factory.MessageBus.InitializationDuration
        }.Min();
        
        var latestEnd = new[] { 
            factory.Redis.InitializedAt,
            factory.Sql.InitializedAt,
            factory.MessageBus.InitializedAt
        }.Max();
        
        var actualTotalTime = latestEnd - earliestStart;
        
        Console.WriteLine($"Sequential time would be: {totalSequentialTime.TotalMilliseconds}ms");
        Console.WriteLine($"Actual total time: {actualTotalTime.TotalMilliseconds}ms");
        Console.WriteLine($"Time saved by parallel initialization: {(totalSequentialTime - actualTotalTime).TotalMilliseconds}ms");
        
        // Verify parallel execution: actual time should be significantly less than sequential time
        // Allow some margin for thread scheduling overhead
        await Assert.That(actualTotalTime.TotalMilliseconds).IsLessThan(totalSequentialTime.TotalMilliseconds * 0.8);
    }
    
    // Test with nested properties that also benefit from parallel initialization
    public class ComplexWebFactory
    {
        [MethodDataSource<ComplexWebFactory>(nameof(GetDatabaseCluster))]
        public required DatabaseCluster Database { get; set; }
        
        [MethodDataSource<ComplexWebFactory>(nameof(GetCacheCluster))]
        public required CacheCluster Cache { get; set; }
        
        public static Func<DatabaseCluster> GetDatabaseCluster()
        {
            return () => new DatabaseCluster { PrimarySql = null!, SecondarySql = null! };
        }
        
        public static Func<CacheCluster> GetCacheCluster()
        {
            return () => new CacheCluster { PrimaryRedis = null!, SecondaryRedis = null! };
        }
    }
    
    public class DatabaseCluster
    {
        [MethodDataSource<DatabaseCluster>(nameof(GetPrimarySql))]
        public required SqlContainer PrimarySql { get; set; }
        
        [MethodDataSource<DatabaseCluster>(nameof(GetSecondarySql))]
        public required SqlContainer SecondarySql { get; set; }
        
        public static Func<SqlContainer> GetPrimarySql()
        {
            return () => new SqlContainer();
        }
        
        public static Func<SqlContainer> GetSecondarySql()
        {
            return () => new SqlContainer();
        }
    }
    
    public class CacheCluster
    {
        [MethodDataSource<CacheCluster>(nameof(GetPrimaryRedis))]
        public required RedisContainer PrimaryRedis { get; set; }
        
        [MethodDataSource<CacheCluster>(nameof(GetSecondaryRedis))]
        public required RedisContainer SecondaryRedis { get; set; }
        
        public static Func<RedisContainer> GetPrimaryRedis()
        {
            return () => new RedisContainer();
        }
        
        public static Func<RedisContainer> GetSecondaryRedis()
        {
            return () => new RedisContainer();
        }
    }
    
    [Test]
    [ClassDataSource<ComplexWebFactory>]
    public async Task Test_NestedParallelPropertyInitialization_ShouldInitializeAllLevelsInParallel(ComplexWebFactory factory)
    {
        // Verify all nested containers are initialized
        await Assert.That(factory.Database).IsNotNull();
        await Assert.That(factory.Database.PrimarySql).IsNotNull();
        await Assert.That(factory.Database.SecondarySql).IsNotNull();
        await Assert.That(factory.Cache).IsNotNull();
        await Assert.That(factory.Cache.PrimaryRedis).IsNotNull();
        await Assert.That(factory.Cache.SecondaryRedis).IsNotNull();
        
        // Check initialization times
        var allContainers = new List<(string name, DateTime initializedAt, TimeSpan duration)>
        {
            ("Database.PrimarySql", factory.Database.PrimarySql.InitializedAt, factory.Database.PrimarySql.InitializationDuration),
            ("Database.SecondarySql", factory.Database.SecondarySql.InitializedAt, factory.Database.SecondarySql.InitializationDuration),
            ("Cache.PrimaryRedis", factory.Cache.PrimaryRedis.InitializedAt, factory.Cache.PrimaryRedis.InitializationDuration),
            ("Cache.SecondaryRedis", factory.Cache.SecondaryRedis.InitializedAt, factory.Cache.SecondaryRedis.InitializationDuration)
        };
        
        foreach (var container in allContainers)
        {
            Console.WriteLine($"{container.name} initialized at {container.initializedAt:HH:mm:ss.fff}, took {container.duration.TotalMilliseconds}ms");
        }
        
        // Calculate potential sequential vs actual time
        var totalSequentialTime = allContainers.Sum(c => c.duration.TotalMilliseconds);
        var earliestStart = allContainers.Min(c => c.initializedAt - c.duration);
        var latestEnd = allContainers.Max(c => c.initializedAt);
        var actualTotalTime = (latestEnd - earliestStart).TotalMilliseconds;
        
        Console.WriteLine($"Sequential time would be: {totalSequentialTime}ms");
        Console.WriteLine($"Actual total time: {actualTotalTime}ms");
        Console.WriteLine($"Time saved by parallel initialization: {totalSequentialTime - actualTotalTime}ms");
        
        // Properties at the same level should be initialized in parallel
        await Assert.That(actualTotalTime).IsLessThan(totalSequentialTime * 0.8);
    }
}