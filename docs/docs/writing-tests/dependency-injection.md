
<!-- doc-test-shared -->

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
        await Assert.That(dep).IsNotNull();
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

### Scope Lifetime and Disposal

- **When the scope is disposed.** TUnit disposes a disposable scope (`IDisposable` or `IAsyncDisposable`) once, when its test ends, after the test class instance is disposed. A retried test keeps the same scope and the same resolved services for every attempt, and the scope is disposed after the last attempt. Each `[Repeat]` iteration is a separate test with its own scope, so services and their state do not carry over between repeats. A test that is skipped, skipped because a dependency failed, cancelled before it starts, or removed by circular-dependency detection still has its scope disposed.
- **Lifetime change for custom callbacks.** Discovery-owned `OnDispose` callbacks now run at session cleanup with a null `TestContext`. Custom handlers must avoid dereferencing that argument when their data source is used for static properties, generic inference, or temporary method-data-source instances. Overlapping sessions share generated and reflection static-property values and retain their scopes until the final owning session finishes.
- **Scopes shared during discovery.** Scopes used to initialize static properties, infer generic class arguments, or construct a temporary instance for an instance method data source live until session cleanup. This includes the temporary instance’s constructor and injected-property scopes: several method rows can borrow their services, so the first test must not dispose them. These scopes are cleaned up even when no test executes. Large data-driven suites can retain more services and other scope-owned resources until session cleanup. Their `OnDispose` handlers receive a null `TestContext`, because no single test owns them.
- **`OnDispose` handlers.** The scope is disposed by an `OnDispose` handler. `OnDispose` is raised once per test, after its final attempt and after the test class instance is disposed. Earlier versions raised it once per attempt, before the instance was disposed, so a handler that used the test class instance must move that work to an `[After(Test)]` hook or to the instance's own `Dispose`.

## Choosing Between the Two

| Need | Use |
|------|-----|
| Full DI container with scoped lifetimes | `DependencyInjectionDataSourceAttribute<TScope>` |
| Simple manual construction or lightweight container | `IClassConstructor` |
| Disposal / cleanup after tests | Either — implement `IAsyncDisposable` on the scope or use event-subscribing interfaces on the constructor |
