using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class CombinedDataSourceTests
{
    public static IEnumerable<string> GetStrings()
    {
        yield return "Hello";
        yield return "World";
    }

    public static IEnumerable<int> GetNumbers()
    {
        yield return 10;
        yield return 20;
        yield return 30;
    }

    public static IEnumerable<bool> GetBools()
    {
        yield return true;
        yield return false;
    }

    #region Basic Tests - Arguments Only

    [Test]
    [CombinedDataSources]
    public async Task TwoParameters_Arguments(
        [Arguments(1, 2, 3)] int x,
        [Arguments("a", "b")] string y)
    {
        // Should create 3 × 2 = 6 test cases
        await Assert.That(x).IsIn([1, 2, 3]);
        await Assert.That(y).IsIn(["a", "b"]);
    }

    [Test]
    [CombinedDataSources]
    public async Task ThreeParameters_Arguments(
        [Arguments(1, 2)] int x,
        [Arguments("a", "b", "c")] string y,
        [Arguments(true, false)] bool z)
    {
        // Should create 2 × 3 × 2 = 12 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn(["a", "b", "c"]);
        await Assert.That(z).IsIn([true, false]);
    }

    [Test]
    [CombinedDataSources]
    public async Task FourParameters_Arguments(
        [Arguments(1, 2)] int w,
        [Arguments("a", "b")] string x,
        [Arguments(true, false)] bool y,
        [Arguments(0.5, 1.5)] double z)
    {
        // Should create 2 × 2 × 2 × 2 = 16 test cases
        await Assert.That(w).IsIn([1, 2]);
        await Assert.That(x).IsIn(["a", "b"]);
        await Assert.That(y).IsIn([true, false]);
        await Assert.That(z).IsIn([0.5, 1.5]);
    }

    #endregion

    #region Mixing Arguments with MethodDataSource

    [Test]
    [CombinedDataSources]
    public async Task ArgumentsWithMethodDataSource(
        [Arguments(1, 2)] int x,
        [MethodDataSource(nameof(GetStrings))] string y)
    {
        // Should create 2 × 2 = 4 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn(["Hello", "World"]);
    }

    [Test]
    [CombinedDataSources]
    public async Task MultipleMethodDataSources(
        [MethodDataSource(nameof(GetNumbers))] int x,
        [MethodDataSource(nameof(GetStrings))] string y)
    {
        // Should create 3 × 2 = 6 test cases
        await Assert.That(x).IsIn([10, 20, 30]);
        await Assert.That(y).IsIn(["Hello", "World"]);
    }

    [Test]
    [CombinedDataSources]
    public async Task ThreeWayMix_ArgumentsAndMethodDataSources(
        [Arguments(1, 2)] int x,
        [MethodDataSource(nameof(GetStrings))] string y,
        [MethodDataSource(nameof(GetBools))] bool z)
    {
        // Should create 2 × 2 × 2 = 8 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn(["Hello", "World"]);
        await Assert.That(z).IsIn([true, false]);
    }

    #endregion

    #region Multiple Attributes on Same Parameter

    [Test]
    [CombinedDataSources]
    public async Task MultipleArgumentsAttributesOnSameParameter(
        [Arguments(1, 2)]
        [Arguments(3, 4)] int x,
        [Arguments("a")] string y)
    {
        // Should create (2 + 2) × 1 = 4 test cases
        await Assert.That(x).IsIn([1, 2, 3, 4]);
        await Assert.That(y).IsEqualTo("a");
    }

    [Test]
    [CombinedDataSources]
    public async Task MixingMultipleDataSourcesPerParameter(
        [Arguments(1)]
        [MethodDataSource(nameof(GetNumbers))] int x,
        [Arguments("test")] string y)
    {
        // Should create (1 + 3) × 1 = 4 test cases
        await Assert.That(x).IsIn([1, 10, 20, 30]);
        await Assert.That(y).IsEqualTo("test");
    }

    #endregion

    #region Type Variety Tests

    [Test]
    [CombinedDataSources]
    public async Task DifferentPrimitiveTypes(
        [Arguments(1, 2)] int intVal,
        [Arguments("a", "b")] string stringVal,
        [Arguments(1.5, 2.5)] double doubleVal,
        [Arguments(true, false)] bool boolVal,
        [Arguments('x', 'y')] char charVal)
    {
        // Should create 2 × 2 × 2 × 2 × 2 = 32 test cases
        await Assert.That(intVal).IsIn([1, 2]);
        await Assert.That(stringVal).IsIn(["a", "b"]);
        await Assert.That(doubleVal).IsIn([1.5, 2.5]);
        await Assert.That(boolVal).IsIn([true, false]);
        await Assert.That(charVal).IsIn(['x', 'y']);
    }

    [Test]
    [CombinedDataSources]
    public async Task NullableTypes(
        [Arguments(1, 2, null)] int? nullableInt,
        [Arguments("a", null)] string? nullableString)
    {
        // Should create 3 × 2 = 6 test cases
        if (nullableInt.HasValue)
        {
            await Assert.That(nullableInt.Value).IsIn([1, 2]);
        }
        // nullableString can be "a" or null
    }

    #endregion

    #region Edge Cases

    [Test]
    [CombinedDataSources]
    public async Task SingleParameterWithSingleValue(
        [Arguments(42)] int x)
    {
        // Should create 1 test case
        await Assert.That(x).IsEqualTo(42);
    }

    [Test]
    [CombinedDataSources]
    public async Task SingleParameterWithMultipleValues(
        [Arguments(1, 2, 3, 4, 5)] int x)
    {
        // Should create 5 test cases
        await Assert.That(x).IsIn([1, 2, 3, 4, 5]);
    }

    [Test]
    [CombinedDataSources]
    public async Task ManyParametersSmallSets(
        [Arguments(1)] int a,
        [Arguments(2)] int b,
        [Arguments(3)] int c,
        [Arguments(4)] int d,
        [Arguments(5)] int e)
    {
        // Should create 1 × 1 × 1 × 1 × 1 = 1 test case
        await Assert.That(a).IsEqualTo(1);
        await Assert.That(b).IsEqualTo(2);
        await Assert.That(c).IsEqualTo(3);
        await Assert.That(d).IsEqualTo(4);
        await Assert.That(e).IsEqualTo(5);
    }

    #endregion

    #region ClassDataSource Tests

    public class SimpleClass
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    [Test]
    [CombinedDataSources]
    public async Task WithClassDataSource(
        [Arguments(1, 2)] int x,
        [ClassDataSource<SimpleClass>] SimpleClass obj)
    {
        // Should create 2 × 1 = 2 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(obj).IsNotNull();
    }

    #endregion

    #region Generic Method Data Source Tests

    public static IEnumerable<T> GetGenericValues<T>(T first, T second)
    {
        yield return first;
        yield return second;
    }

    // Note: MethodDataSource with generic parameters and arguments needs special syntax
    // This test is simplified for now
    [Test]
    [CombinedDataSources]
    public async Task WithTypedMethodDataSource(
        [Arguments(1, 2)] int x,
        [MethodDataSource<CombinedDataSourceTests>(nameof(GetNumbers))] int y)
    {
        // Should create 2 × 3 = 6 test cases
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn([10, 20, 30]);
    }

    #endregion

    #region Verification Tests - Ensure Correct Combinations

    private static readonly HashSet<string> _seenCombinations = new();
    private static readonly Lock _lock = new();

    [Test]
    [CombinedDataSources]
    public async Task VerifyCartesianProduct_TwoParameters(
        [Arguments("A", "B")] string x,
        [Arguments(1, 2, 3)] int y)
    {
        // Should create 2 × 3 = 6 unique combinations
        var combination = $"{x}-{y}";

        bool isUnique;
        lock (_lock)
        {
            isUnique = _seenCombinations.Add(combination);
        }

        await Assert.That(isUnique).IsTrue();
        await Assert.That(x).IsIn(["A", "B"]);
        await Assert.That(y).IsIn([1, 2, 3]);
    }

    #endregion

    #region Performance Test - Many Combinations

    [Test]
    [CombinedDataSources]
    public async Task LargeCartesianProduct(
        [Arguments(1, 2, 3, 4, 5)] int a,
        [Arguments(1, 2, 3, 4)] int b,
        [Arguments(1, 2, 3)] int c)
    {
        // Should create 5 × 4 × 3 = 60 test cases
        await Assert.That(a).IsIn([1, 2, 3, 4, 5]);
        await Assert.That(b).IsIn([1, 2, 3, 4]);
        await Assert.That(c).IsIn([1, 2, 3]);
    }

    #endregion

    #region Property Injection Tests

    public class PersonWithPropertyInjection
    {
        [MethodDataSource<PersonWithPropertyInjection>(nameof(GetAddressData))]
        public required Address Address { get; set; }

        public string Name { get; set; } = string.Empty;

        public static Func<Address> GetAddressData()
        {
            return () => new Address
            {
                Street = "123 Main St",
                City = "TestCity",
                ZipCode = "12345"
            };
        }
    }

    public class Address
    {
        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string ZipCode { get; set; } = string.Empty;
    }

    [Test]
    [CombinedDataSources]
    public async Task CombinedDataSource_WithPropertyInjection_SingleLevel(
        [Arguments(1, 2)] int x,
        [ClassDataSource<PersonWithPropertyInjection>] PersonWithPropertyInjection person)
    {
        // Should create 2 test cases (one for each x value)
        // Each person instance should have Address property injected
        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(person).IsNotNull();
        await Assert.That(person.Address).IsNotNull();
        await Assert.That(person.Address.Street).IsEqualTo("123 Main St");
        await Assert.That(person.Address.City).IsEqualTo("TestCity");
        await Assert.That(person.Address.ZipCode).IsEqualTo("12345");
    }

    public class PersonWithNestedPropertyInjection
    {
        [MethodDataSource<PersonWithNestedPropertyInjection>(nameof(GetAddressWithCountryData))]
        public required AddressWithCountry Address { get; set; }

        public string Name { get; set; } = string.Empty;

        public static Func<AddressWithCountry> GetAddressWithCountryData()
        {
            return () => new AddressWithCountry
            {
                Street = "456 Oak Ave",
                City = "Springfield",
                Country = null!
            };
        }
    }

    public class AddressWithCountry
    {
        [MethodDataSource<AddressWithCountry>(nameof(GetCountryData))]
        public required Country Country { get; set; }

        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public static Func<Country> GetCountryData()
        {
            return () => new Country { Name = "USA", Code = "US" };
        }
    }

    public class Country
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    [Test]
    [CombinedDataSources]
    public async Task CombinedDataSource_WithNestedPropertyInjection(
        [Arguments("A", "B")] string x,
        [ClassDataSource<PersonWithNestedPropertyInjection>] PersonWithNestedPropertyInjection person)
    {
        // Should create 2 test cases
        // Person → Address → Country should all be properly injected
        await Assert.That(x).IsIn(["A", "B"]);
        await Assert.That(person).IsNotNull();
        await Assert.That(person.Address).IsNotNull();
        await Assert.That(person.Address.Street).IsEqualTo("456 Oak Ave");
        await Assert.That(person.Address.City).IsEqualTo("Springfield");

        // Verify nested property injection worked
        await Assert.That(person.Address.Country).IsNotNull();
        await Assert.That(person.Address.Country.Name).IsEqualTo("USA");
        await Assert.That(person.Address.Country.Code).IsEqualTo("US");
    }

    [Test]
    [CombinedDataSources]
    public async Task CombinedDataSource_MultipleParametersWithPropertyInjection(
        [Arguments(1, 2)] int x,
        [ClassDataSource<PersonWithPropertyInjection>] PersonWithPropertyInjection person1,
        [ClassDataSource<PersonWithNestedPropertyInjection>] PersonWithNestedPropertyInjection person2)
    {
        // Should create 2 × 1 × 1 = 2 test cases
        // Both person parameters should have their properties injected
        await Assert.That(x).IsIn([1, 2]);

        // Verify person1 property injection
        await Assert.That(person1).IsNotNull();
        await Assert.That(person1.Address).IsNotNull();
        await Assert.That(person1.Address.Street).IsNotEmpty();

        // Verify person2 nested property injection
        await Assert.That(person2).IsNotNull();
        await Assert.That(person2.Address).IsNotNull();
        await Assert.That(person2.Address.Country).IsNotNull();
        await Assert.That(person2.Address.Country.Name).IsNotEmpty();
    }

    #endregion

    #region IAsyncInitializer Tests

    public class InitializableClass : IAsyncInitializer
    {
        public bool IsInitialized { get; private set; }
        public DateTime InitializationTime { get; private set; }
        public int Value { get; set; }

        public async Task InitializeAsync()
        {
            // Simulate async initialization work
            await Task.Delay(10);
            IsInitialized = true;
            InitializationTime = DateTime.UtcNow;
        }
    }

    [Test]
    [CombinedDataSources]
    public async Task CombinedDataSource_WithIAsyncInitializer(
        [Arguments(1, 2, 3)] int x,
        [ClassDataSource<InitializableClass>] InitializableClass obj)
    {
        // Should create 3 test cases
        // obj.InitializeAsync() should have been called before test execution
        await Assert.That(x).IsIn([1, 2, 3]);
        await Assert.That(obj).IsNotNull();
        await Assert.That(obj.IsInitialized).IsTrue();
        await Assert.That(obj.InitializationTime).IsNotEqualTo(default(DateTime));
    }

    public class DatabaseConnection : IAsyncInitializer
    {
        public bool IsConnected { get; private set; }
        public string ConnectionString { get; set; } = "Server=localhost;Database=TestDB";
        public int ConnectionAttempts { get; private set; }

        public async Task InitializeAsync()
        {
            // Simulate database connection
            await Task.Delay(5);
            ConnectionAttempts++;
            IsConnected = true;
        }
    }

    [Test]
    [CombinedDataSources]
    public async Task CombinedDataSource_MultipleParametersWithIAsyncInitializer(
        [Arguments("Query1", "Query2")] string query,
        [ClassDataSource<DatabaseConnection>] DatabaseConnection db)
    {
        // Should create 2 test cases
        // Database connection should be initialized before test runs
        await Assert.That(query).IsIn(["Query1", "Query2"]);
        await Assert.That(db).IsNotNull();
        await Assert.That(db.IsConnected).IsTrue();
        await Assert.That(db.ConnectionAttempts).IsEqualTo(1);
    }

    #endregion

    #region Combined Property Injection and IAsyncInitializer Tests

    public class InitializablePersonWithPropertyInjection : IAsyncInitializer
    {
        [MethodDataSource<InitializablePersonWithPropertyInjection>(nameof(GetConfigData))]
        public required Configuration Config { get; set; }

        public bool IsInitialized { get; private set; }
        public string Name { get; set; } = string.Empty;

        public async Task InitializeAsync()
        {
            // Simulate async initialization that depends on injected properties
            await Task.Delay(5);

            // This verifies that property injection happens BEFORE IAsyncInitializer
            if (Config == null)
            {
                throw new InvalidOperationException("Config should be injected before InitializeAsync is called");
            }

            IsInitialized = true;
        }

        public static Func<Configuration> GetConfigData()
        {
            return () => new Configuration { ApiKey = "test-key-123", Timeout = 30 };
        }
    }

    public class Configuration
    {
        public string ApiKey { get; set; } = string.Empty;
        public int Timeout { get; set; }
    }

    [Test]
    [CombinedDataSources]
    public async Task CombinedDataSource_WithPropertyInjectionAndIAsyncInitializer(
        [Arguments(10, 20)] int x,
        [ClassDataSource<InitializablePersonWithPropertyInjection>] InitializablePersonWithPropertyInjection person)
    {
        // Should create 2 test cases
        // Verify property injection happened
        await Assert.That(person).IsNotNull();
        await Assert.That(person.Config).IsNotNull();
        await Assert.That(person.Config.ApiKey).IsEqualTo("test-key-123");
        await Assert.That(person.Config.Timeout).IsEqualTo(30);

        // Verify IAsyncInitializer was called (after property injection)
        await Assert.That(person.IsInitialized).IsTrue();

        await Assert.That(x).IsIn([10, 20]);
    }

    public class InitializableAddressWithNestedInjection : IAsyncInitializer
    {
        [MethodDataSource<InitializableAddressWithNestedInjection>(nameof(GetLocationData))]
        public required Location Location { get; set; }

        public bool IsValidated { get; private set; }

        public async Task InitializeAsync()
        {
            // Simulate validation that requires the injected Location
            await Task.Delay(5);

            if (Location == null || Location.Coordinates == null)
            {
                throw new InvalidOperationException("Nested property injection should complete before InitializeAsync");
            }

            IsValidated = true;
        }

        public static Func<Location> GetLocationData()
        {
            return () => new Location { Name = "HQ", Coordinates = null! };
        }
    }

    public class Location : IAsyncInitializer
    {
        [MethodDataSource<Location>(nameof(GetCoordinatesData))]
        public required Coordinates Coordinates { get; set; }

        public string Name { get; set; } = string.Empty;
        public bool IsGeolocated { get; private set; }

        public async Task InitializeAsync()
        {
            // Simulate geolocation service call
            await Task.Delay(3);
            IsGeolocated = true;
        }

        public static Func<Coordinates> GetCoordinatesData()
        {
            return () => new Coordinates { Latitude = 37.7749, Longitude = -122.4194 };
        }
    }

    public class Coordinates
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    [Test]
    [CombinedDataSources]
    public async Task CombinedDataSource_WithNestedPropertyInjectionAndMultipleIAsyncInitializers(
        [Arguments(true, false)] bool flag,
        [ClassDataSource<InitializableAddressWithNestedInjection>] InitializableAddressWithNestedInjection address)
    {
        // Should create 2 test cases
        // This tests the complex scenario:
        // 1. Address has nested property injection (Location)
        // 2. Location has nested property injection (Coordinates)
        // 3. Both Address and Location implement IAsyncInitializer
        // 4. Initialization order should be: Coordinates → Location.InitializeAsync() → Address.InitializeAsync()

        await Assert.That(flag).IsIn([true, false]);
        await Assert.That(address).IsNotNull();

        // Verify nested property injection worked
        await Assert.That(address.Location).IsNotNull();
        await Assert.That(address.Location.Coordinates).IsNotNull();
        await Assert.That(address.Location.Coordinates.Latitude).IsEqualTo(37.7749);
        await Assert.That(address.Location.Coordinates.Longitude).IsEqualTo(-122.4194);

        // Verify IAsyncInitializer was called on Location (deepest first)
        await Assert.That(address.Location.IsGeolocated).IsTrue();

        // Verify IAsyncInitializer was called on Address (after nested objects)
        await Assert.That(address.IsValidated).IsTrue();
    }

    [Test]
    [CombinedDataSources]
    public async Task CombinedDataSource_ComplexScenario_MultipleParametersWithMixedFeatures(
        [Arguments(1, 2)] int x,
        [MethodDataSource(nameof(GetStrings))] string y,
        [ClassDataSource<InitializableClass>] InitializableClass initializable,
        [ClassDataSource<PersonWithPropertyInjection>] PersonWithPropertyInjection personWithProps,
        [ClassDataSource<InitializablePersonWithPropertyInjection>] InitializablePersonWithPropertyInjection personWithBoth)
    {
        // Should create 2 × 2 × 1 × 1 × 1 = 4 test cases
        // This tests that CombinedDataSources handles:
        // - Primitive arguments
        // - Method data sources
        // - IAsyncInitializer objects
        // - Property injection objects
        // - Objects with both property injection AND IAsyncInitializer

        await Assert.That(x).IsIn([1, 2]);
        await Assert.That(y).IsIn(["Hello", "World"]);

        // Verify IAsyncInitializer
        await Assert.That(initializable.IsInitialized).IsTrue();

        // Verify property injection
        await Assert.That(personWithProps.Address).IsNotNull();

        // Verify both features together
        await Assert.That(personWithBoth.IsInitialized).IsTrue();
        await Assert.That(personWithBoth.Config).IsNotNull();
    }

    #endregion
}
