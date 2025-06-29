using System;
using System.Collections.Generic;
using TUnit.Core;
using TUnit.Assertions;

namespace TUnit.Examples;

/// <summary>
/// Example demonstrating TUnit's clean architecture where:
/// - Source generator only emits TestMetadata (data structures)
/// - TestBuilder handles all complex runtime logic
/// </summary>
public class CleanArchitectureExample
{
    /// <summary>
    /// Simple test - discovered by TestMetadataGenerator at compile time
    /// </summary>
    [Test]
    public async Task SimpleTest()
    {
        // The source generator found this test and created TestMetadata for it
        // TestBuilder expands it into a TestDefinition at runtime
        var result = 2 + 2;
        await Assert.That(result).IsEqualTo(4);
    }
    
    /// <summary>
    /// Data-driven test with inline arguments
    /// </summary>
    [Test]
    [Arguments(1, 1, 2)]
    [Arguments(2, 3, 5)]
    [Arguments(10, -5, 5)]
    public async Task ParameterizedTest(int a, int b, int expected)
    {
        // Source generator creates TestMetadata with InlineDataSourceProvider
        // TestBuilder enumerates the data source and creates 3 test instances
        var sum = a + b;
        await Assert.That(sum).IsEqualTo(expected);
    }
    
    /// <summary>
    /// Data-driven test with method data source
    /// </summary>
    [Test]
    [MethodDataSource(nameof(GetTestCases))]
    public async Task MethodDataSourceTest(string input, int expectedLength)
    {
        // Source generator creates TestMetadata with MethodDataSourceProvider
        // TestBuilder invokes GetTestCases() at runtime and creates test instances
        await Assert.That(input.Length).IsEqualTo(expectedLength);
    }
    
    public static IEnumerable<(string, int)> GetTestCases()
    {
        yield return ("hello", 5);
        yield return ("world", 5);
        yield return ("TUnit rocks!", 12);
    }
    
    /// <summary>
    /// Test with repeat functionality
    /// </summary>
    [Test]
    [Repeat(3)]
    public async Task RepeatedTest()
    {
        // Source generator sets RepeatCount = 3 in TestMetadata
        // TestBuilder creates 3 instances of this test
        var random = new Random();
        var value = random.Next(1, 100);
        await Assert.That(value).IsGreaterThan(0).And.IsLessThanOrEqualTo(100);
    }
}

/// <summary>
/// Example with class-level data source and constructor injection
/// </summary>
[ClassDataSource(typeof(UserTestData))]
public class UserServiceTests
{
    private readonly User _testUser;
    private readonly UserService _service;
    
    public UserServiceTests(User testUser)
    {
        // Source generator creates TestMetadata with ClassDataSources
        // TestBuilder enumerates UserTestData and injects each user
        _testUser = testUser;
        _service = new UserService();
    }
    
    [Test]
    public async Task ValidateUser_ShouldPass()
    {
        var isValid = _service.Validate(_testUser);
        await Assert.That(isValid).IsTrue();
    }
    
    [Test]
    public async Task GetUserAge_ShouldBePositive()
    {
        var age = _service.CalculateAge(_testUser);
        await Assert.That(age).IsGreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Example with property injection
/// </summary>
public class PropertyInjectionExample
{
    [Arguments("Development")]
    [Arguments("Staging")]
    [Arguments("Production")]
    public string Environment { get; set; } = "";
    
    [Test]
    public async Task EnvironmentSpecificTest()
    {
        // Source generator creates TestMetadata with PropertyDataSources
        // TestBuilder injects the property value before test execution
        await Assert.That(Environment).IsNotNullOrEmpty();
        
        var config = new ConfigService(Environment);
        var connectionString = config.GetConnectionString();
        
        await Assert.That(connectionString).Contains(Environment);
    }
}

/// <summary>
/// Complex example showing how TestBuilder handles tuple unwrapping
/// </summary>
public class TupleUnwrappingExample
{
    [Test]
    [MethodDataSource(nameof(GetComplexTestData))]
    public async Task ComplexDataTest(int id, string name, DateTime birthDate, bool isActive)
    {
        // Source generator sees the method returns tuples
        // TestBuilder unwraps the tuple into individual parameters at runtime
        await Assert.That(id).IsGreaterThan(0);
        await Assert.That(name).IsNotNullOrEmpty();
        await Assert.That(birthDate).IsLessThan(DateTime.Now);
        await Assert.That(isActive).IsNotNull();
    }
    
    public static IEnumerable<(int, string, DateTime, bool)> GetComplexTestData()
    {
        yield return (1, "Alice", new DateTime(1990, 1, 1), true);
        yield return (2, "Bob", new DateTime(1985, 6, 15), false);
        yield return (3, "Charlie", new DateTime(2000, 12, 31), true);
    }
}

// Supporting classes for examples
public class User
{
    public string Name { get; set; } = "";
    public DateTime BirthDate { get; set; }
}

public class UserService
{
    public bool Validate(User user) => !string.IsNullOrEmpty(user.Name);
    public int CalculateAge(User user) => DateTime.Now.Year - user.BirthDate.Year;
}

public class UserTestData : IEnumerable<User>
{
    public IEnumerator<User> GetEnumerator()
    {
        yield return new User { Name = "Test User 1", BirthDate = new DateTime(1990, 1, 1) };
        yield return new User { Name = "Test User 2", BirthDate = new DateTime(2000, 6, 15) };
    }
    
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

public class ConfigService
{
    private readonly string _environment;
    
    public ConfigService(string environment) => _environment = environment;
    
    public string GetConnectionString() => $"Server=db.{_environment.ToLower()}.example.com;Database=MyApp";
}

/// <summary>
/// Summary of the clean architecture:
/// 
/// 1. Source Generation Phase (Compile Time):
///    - TestMetadataGenerator scans for [Test] attributes
///    - Emits only TestMetadata data structures
///    - No complex logic or execution code generated
/// 
/// 2. Runtime Phase:
///    - TestSourceRegistrar registers TestMetadata
///    - TestBuilder expands metadata into executable tests
///    - Handles all complex logic:
///      * Data source enumeration
///      * Tuple unwrapping
///      * Property injection
///      * Constructor parameter resolution
///      * Test instance creation
/// 
/// Benefits:
/// - Simpler source generator (easier to maintain)
/// - Better debugging (step through actual code, not generated strings)
/// - Improved performance (expression compilation, caching)
/// - Clear separation of concerns
/// </summary>