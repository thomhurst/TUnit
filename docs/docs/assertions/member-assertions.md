# Member Assertions

The `.Member()` method allows you to assert on object properties while maintaining the parent object's context for chaining. This is useful when you need to validate multiple properties of the same object.

## Basic Usage

```csharp
[Test]
public async Task BasicMemberAssertions()
{
    var user = await GetUserAsync();

    // Assert on a single property
    await Assert.That(user)
        .Member(u => u.Email, email => email.IsEqualTo("user@example.com"));

    // Chain multiple property assertions
    await Assert.That(user)
        .Member(u => u.FirstName, name => name.IsEqualTo("John"))
        .And.Member(u => u.LastName, name => name.IsEqualTo("Doe"))
        .And.Member(u => u.Age, age => age.IsGreaterThan(18));
}
```

## Why Use Member Assertions?

The key advantage of `.Member()` is that it returns to the parent context after each assertion, allowing you to chain multiple property checks:

```csharp
[Test]
public async Task MemberAssertionsWithFullContext()
{
    var order = await GetOrderAsync();

    // Each .Member() call works on the order object
    await Assert.That(order)
        .IsNotNull()
        .And.Member(o => o.OrderId, id => id.IsGreaterThan(0))
        .And.Member(o => o.Status, status => status.IsEqualTo(OrderStatus.Pending))
        .And.Member(o => o.Total, total => total.IsGreaterThan(0));
}
```

## Nested Properties

Member assertions support nested properties:

```csharp
[Test]
public async Task NestedPropertyAssertions()
{
    var customer = await GetCustomerAsync();

    // Access nested properties directly
    await Assert.That(customer)
        .Member(c => c.Address.Street, street => street.IsNotNull())
        .And.Member(c => c.Address.City, city => city.IsEqualTo("Seattle"))
        .And.Member(c => c.Address.ZipCode, zip => zip.Matches(@"^\d{5}$"));
}
```

## Complex Assertions on Members

You can perform complex assertions on member values, including collections:

```csharp
[Test]
public async Task ComplexMemberAssertions()
{
    var team = await GetTeamAsync();

    await Assert.That(team)
        .Member(t => t.Name, name => name.StartsWith("Team"))
        .And.Member(t => t.Members, members => members
            .HasCount().IsGreaterThan(0)
            .And.All(m => m.IsActive)
            .And.Any(m => m.Role == "Lead"))
        .And.Member(t => t.CreatedDate, date => date
            .IsGreaterThan(DateTime.UtcNow.AddYears(-1)));
}
```

## Using Or Logic

Member assertions work with both `.And` and `.Or` combinators:

```csharp
[Test]
public async Task MemberAssertionsWithOrLogic()
{
    var product = await GetProductAsync();

    // Use Or to check alternative conditions
    await Assert.That(product)
        .Member(p => p.Status, status => status.IsEqualTo(ProductStatus.Active))
        .Or.Member(p => p.Status, status => status.IsEqualTo(ProductStatus.Preview));

    // Mix And and Or for complex logic
    await Assert.That(product)
        .Member(p => p.Price, price => price.IsGreaterThan(0))
        .And.Member(p => p.Stock, stock => stock.IsGreaterThan(0))
        .Or.Member(p => p.BackorderAllowed, allowed => allowed.IsTrue());
}
```

## Complete Example

```csharp
[Test]
public async Task ComplexObjectValidation()
{
    var user = await GetUserAsync("john.doe");

    // Chain multiple member assertions
    await Assert.That(user)
        .IsNotNull()
        .And.Member(u => u.Email, email => email.IsEqualTo("john.doe@example.com"))
        .And.Member(u => u.Age, age => age.IsGreaterThan(18))
        .And.Member(u => u.Roles, roles => roles.Contains("Admin"));
}
```

## Nested Object Assertions

```csharp
[Test]
public async Task NestedObjectAssertions()
{
    var company = await GetCompanyAsync();

    await Assert.That(company)
        .IsNotNull()
        .And.Member(c => c.Name, name => name.IsEqualTo("TechCorp"))
        .And.Member(c => c.Address.City, city => city.IsEqualTo("Seattle"))
        .And.Member(c => c.Address.ZipCode, zip => zip.Matches(@"^\d{5}$"))
        .And.Member(c => c.Employees, employees => employees
            .HasCount().IsBetween(100, 500)
            .And.All(e => e.Email.EndsWith("@techcorp.com")));
}
```
