---
sidebar_position: 11
---

# Mocking

TUnit is a testing framework focused on test execution and does not include any built-in mocking capabilities. This gives you the freedom to choose whichever mocking library best suits your needs and preferences.

## Popular Mocking Libraries

You can use any .NET mocking library with TUnit. Here are some popular options:

### NSubstitute
A friendly substitute for .NET mocking frameworks with a simple, fluent syntax.

```csharp
[Test]
public async Task Calculator_Add_CallsLogger()
{
    // Arrange
    var logger = Substitute.For<ILogger>();
    var calculator = new Calculator(logger);
    
    // Act
    calculator.Add(2, 3);
    
    // Assert
    logger.Received().Log("Adding 2 + 3");
}
```

### Moq
The most popular and friendly mocking library for .NET.

```csharp
[Test]
public async Task Calculator_Add_CallsLogger()
{
    // Arrange
    var loggerMock = new Mock<ILogger>();
    var calculator = new Calculator(loggerMock.Object);
    
    // Act
    calculator.Add(2, 3);
    
    // Assert
    loggerMock.Verify(x => x.Log("Adding 2 + 3"), Times.Once);
}
```

### FakeItEasy
A simple mocking library for .NET with a focus on ease of use.

```csharp
[Test]
public async Task Calculator_Add_CallsLogger()
{
    // Arrange
    var logger = A.Fake<ILogger>();
    var calculator = new Calculator(logger);
    
    // Act
    calculator.Add(2, 3);
    
    // Assert
    A.CallTo(() => logger.Log("Adding 2 + 3")).MustHaveHappened();
}
```

## Installation

To use a mocking library with TUnit, simply install it via NuGet alongside TUnit:

```bash
# For NSubstitute
dotnet add package NSubstitute

# For Moq
dotnet add package Moq

# For FakeItEasy
dotnet add package FakeItEasy
```

## Best Practices

1. **Choose one mocking library per project** - Consistency across your test suite makes it easier for team members to understand and maintain tests.

2. **Use dependency injection** - TUnit's support for [dependency injection](./dependency-injection.md) works well with mocked dependencies.

3. **Keep mocks simple** - Only mock what you need for the specific test. Over-mocking can make tests brittle and hard to understand.

4. **Consider using real implementations** - Sometimes using real implementations (especially for simple objects) can make tests more maintainable than mocks.

## Example with Dependency Injection

You can combine TUnit's dependency injection with your preferred mocking library:

```csharp
public class ServiceTests
{
    private readonly IRepository _repository;
    private readonly ServiceUnderTest _service;

    public ServiceTests()
    {
        // Using NSubstitute as an example
        _repository = Substitute.For<IRepository>();
        _service = new ServiceUnderTest(_repository);
    }

    [Test]
    public async Task GetUser_ReturnsUserFromRepository()
    {
        // Arrange
        var expectedUser = new User { Id = 1, Name = "John" };
        _repository.GetUserAsync(1).Returns(expectedUser);
        
        // Act
        var result = await _service.GetUserAsync(1);
        
        // Assert
        await Assert.That(result).IsEqualTo(expectedUser);
    }
}
```

## Resources

- [NSubstitute Documentation](https://nsubstitute.github.io/)
- [Moq Documentation](https://github.com/moq/moq4)
- [FakeItEasy Documentation](https://fakeiteasy.github.io/)