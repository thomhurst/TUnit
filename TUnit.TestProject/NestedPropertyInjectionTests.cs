using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[EngineTest(ExpectedResult.Pass)]
public class NestedPropertyInjectionTests
{
    public class PersonWithDataSource
    {
        [MethodDataSource<PersonWithDataSource>(nameof(GetAddressData))]
        public required Address Address { get; set; }

        public string Name { get; set; } = string.Empty;

        public static IEnumerable<Func<Address>> GetAddressData()
        {
            yield return () => new Address
            {
                Street = "123 Main St",
                City = "TestCity",
                Country = null!
            };
            yield return () => new Address
            {
                Street = "456 Oak Ave",
                City = "AnotherCity",
                Country = null!
            };
        }
    }

    public class Address
    {
        [MethodDataSource<Address>(nameof(GetCountryData))]
        public required Country Country { get; set; }

        public string Street { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;

        public static IEnumerable<Func<Country>> GetCountryData()
        {
            yield return () => new Country { Name = "USA", Code = "US" };
            yield return () => new Country { Name = "Canada", Code = "CA" };
        }
    }

    public class Country
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
    }

    [Test]
    [ClassDataSource<PersonWithDataSource>]
    public async Task Test_NestedPropertyInjection_InConstructorArguments(PersonWithDataSource person)
    {
        // Verify the person object was injected properly
        await Assert.That(person).IsNotNull();
        await Assert.That(person.Address).IsNotNull();
        await Assert.That(person.Address.Country).IsNotNull();

        // Verify nested properties were injected
        await Assert.That(person.Address.Street).IsNotNull().And.IsNotEmpty();
        await Assert.That(person.Address.City).IsNotNull().And.IsNotEmpty();
        await Assert.That(person.Address.Country.Name).IsNotNull().And.IsNotEmpty();
        await Assert.That(person.Address.Country.Code).IsNotNull().And.IsNotEmpty();

        Console.WriteLine($"Person: {person.Name}");
        Console.WriteLine($"Address: {person.Address.Street}, {person.Address.City}");
        Console.WriteLine($"Country: {person.Address.Country.Name} ({person.Address.Country.Code})");
    }

    [Test]
    [MethodDataSource(nameof(GetPersonWithDataSourceData))]
    public async Task Test_NestedPropertyInjection_InMethodArguments(PersonWithDataSource person)
    {
        // Same test but person comes from method argument instead of constructor
        await Assert.That(person).IsNotNull();
        await Assert.That(person.Address).IsNotNull();
        await Assert.That(person.Address.Country).IsNotNull();

        // Verify nested properties were injected
        await Assert.That(person.Address.Street).IsNotNull().And.IsNotEmpty();
        await Assert.That(person.Address.City).IsNotNull().And.IsNotEmpty();
        await Assert.That(person.Address.Country.Name).IsNotNull().And.IsNotEmpty();
        await Assert.That(person.Address.Country.Code).IsNotNull().And.IsNotEmpty();

        Console.WriteLine($"Method arg - Person: {person.Name}");
        Console.WriteLine($"Method arg - Address: {person.Address.Street}, {person.Address.City}");
        Console.WriteLine($"Method arg - Country: {person.Address.Country.Name} ({person.Address.Country.Code})");
    }

    public static IEnumerable<Func<PersonWithDataSource>> GetPersonWithDataSourceData()
    {
        yield return () => new PersonWithDataSource { Name = "John Doe", Address = null! };
        yield return () => new PersonWithDataSource { Name = "Jane Smith", Address = null! };
    }
}
