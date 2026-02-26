# Dependency Injection

TUnit provides two mechanisms for controlling how test classes are constructed: the low-level `IClassConstructor` interface and the higher-level `DependencyInjectionDataSourceAttribute<TScope>` helper. Both are registered via attributes and give full control over how constructor arguments are resolved.

## IClassConstructor

The `IClassConstructor` interface gives direct control over how test class instances are created. Implement this interface when you need custom instantiation logic — for example, resolving dependencies from a lightweight container, applying decorators, or wrapping construction in a factory.

Register it with `[ClassConstructor<T>]` on the test class. Each test gets its own attribute instance, so you can safely store per-test state.

```csharp
public class CustomConstructor : IClassConstructor
{
    public Task<object> Create(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type,
        ClassConstructorMetadata classConstructorMetadata)
    {
        // Resolve the type however you like — manual construction, a container, etc.
        return Task.FromResult(Activator.CreateInstance(type)!);
    }
}

[ClassConstructor<CustomConstructor>]
public class MyTestClass(SomeDependency dep)
{
    [Test]
    public async Task MyTest()
    {
        // dep was provided by CustomConstructor.Create()
    }
}
```

The `ClassConstructorMetadata` parameter provides context about the test being constructed, including the test's data-source arguments and metadata. You can also implement [event-subscribing interfaces](event-subscribing.md) on the same class to get notified when a test finishes — useful for disposing objects after the test completes.

## DependencyInjectionDataSourceAttribute

For DI-container integration, TUnit provides `DependencyInjectionDataSourceAttribute<TScope>` — an abstract base class that handles scope lifecycle automatically. You supply two methods:

1. **`CreateScope`** — create a DI scope (called once per test class instance)
2. **`Create`** — resolve a service from that scope by type

### Microsoft.Extensions.DependencyInjection Example

```csharp
public class MicrosoftDIAttribute : DependencyInjectionDataSourceAttribute<IServiceScope>
{
    private static readonly IServiceProvider ServiceProvider = BuildProvider();

    public override IServiceScope CreateScope(DataGeneratorMetadata dataGeneratorMetadata)
    {
        return ServiceProvider.CreateScope();
    }

    public override object? Create(IServiceScope scope, Type type)
    {
        return scope.ServiceProvider.GetService(type);
    }

    private static IServiceProvider BuildProvider()
    {
        return new ServiceCollection()
            .AddSingleton<IUserRepository, UserRepository>()
            .AddTransient<IEmailService, FakeEmailService>()
            .BuildServiceProvider();
    }
}
```

Apply the attribute to a test class that accepts constructor parameters. TUnit resolves each parameter through the `Create` method:

```csharp
[MicrosoftDI]
public class UserServiceTests(IUserRepository repo, IEmailService email)
{
    [Test]
    public async Task CreateUser_SendsWelcomeEmail()
    {
        var service = new UserService(repo, email);
        await service.CreateAsync("alice@example.com");

        await Assert.That(((FakeEmailService)email).SentCount).IsEqualTo(1);
    }
}
```

### Other Containers

The same pattern works with any DI container. Replace `IServiceScope` with whatever scope type the container uses (e.g., `ILifetimeScope` for Autofac) and implement `CreateScope` / `Create` accordingly.

## Choosing Between the Two

| Need | Use |
|------|-----|
| Full DI container with scoped lifetimes | `DependencyInjectionDataSourceAttribute<TScope>` |
| Simple manual construction or lightweight container | `IClassConstructor` |
| Disposal / cleanup after tests | Either — implement `IAsyncDisposable` on the scope or use event-subscribing interfaces on the constructor |
