using TUnit.Core.Enums;
using TUnit.Core.Tracking;
using TUnit.Engine.Services.TestExecution;

namespace TUnit.UnitTests;

/// <summary>
/// A dependency injection data source for a static property runs against
/// <see cref="TestSessionContext.GlobalStaticPropertyContext"/>, which no test owns. The object tracker
/// does not dispose the services a disposable scope owns, so the scope must be disposed at session end,
/// once, by raising that context's OnDispose callbacks (#7205).
/// </summary>
public class StaticPropertyScopeDisposalTests
{
    [Test]
    public async Task Session_OnDispose_Disposes_A_Static_Property_Scope_Once()
    {
        var context = new TestBuilderContext { TestMetadata = TestSessionContext.GlobalStaticPropertyContext.TestMetadata };
        var attribute = new ScopeAttribute();

        object? created = null;
        await foreach (var row in ((IDataSourceAttribute) attribute).GetDataRowsAsync(CreateMetadata(context)))
        {
            created = (await row())![0];
            break;
        }

        var service = (Service) created!;
        await Assert.That(ObjectTracker.IsExternallyOwned(service)).IsTrue();
        await Assert.That(service.DisposeCount).IsEqualTo(0);

        var exceptions = new List<Exception>();
        await TestOnDisposeRaiser.RaiseForSessionAsync(context, exceptions);
        await TestOnDisposeRaiser.RaiseForSessionAsync(context, exceptions);

        await Assert.That(exceptions).IsEmpty();
        await Assert.That(service.Scope.DisposeCount).IsEqualTo(1);
        await Assert.That(service.DisposeCount).IsEqualTo(1);
    }

    [Test]
    public async Task Session_OnDispose_Collects_Handler_Exceptions()
    {
        var context = new TestBuilderContext { TestMetadata = TestSessionContext.GlobalStaticPropertyContext.TestMetadata };
        var ranAfterFailure = false;
        context.Events.OnDispose += (_, _) => throw new InvalidOperationException("dispose failed");
        context.Events.OnDispose += (_, _) =>
        {
            ranAfterFailure = true;
            return default;
        };

        var exceptions = new List<Exception>();
        await TestOnDisposeRaiser.RaiseForSessionAsync(context, exceptions);

        await Assert.That(exceptions).HasSingleItem();
        await Assert.That(ranAfterFailure).IsTrue();
    }

    private static DataGeneratorMetadata CreateMetadata(TestBuilderContext context) => new()
    {
        TestBuilderContext = new TestBuilderContextAccessor(context),
        MembersToGenerate =
        [
            new ParameterMetadata(typeof(Service))
            {
                Name = "service",
                TypeInfo = new ConcreteType(typeof(Service)),
                ReflectionInfo = null!,
                IsNullable = false
            }
        ],
        TestInformation = null,
        Type = DataGeneratorType.Property,
        TestSessionId = string.Empty,
        TestClassInstance = null,
        ClassInstanceArguments = null,
    };

    private sealed class Scope : IDisposable
    {
        private readonly List<Service> _services = [];
        private int _disposeCount;

        public int DisposeCount => _disposeCount;

        public Service Resolve()
        {
            var service = new Service(this);
            _services.Add(service);
            return service;
        }

        public void Dispose()
        {
            Interlocked.Increment(ref _disposeCount);

            foreach (var service in _services)
            {
                service.Dispose();
            }
        }
    }

    private sealed class Service(Scope scope) : IDisposable
    {
        private int _disposeCount;

        public Scope Scope { get; } = scope;

        public int DisposeCount => _disposeCount;

        public void Dispose() => Interlocked.Increment(ref _disposeCount);
    }

    private sealed class ScopeAttribute : DependencyInjectionDataSourceAttribute<Scope>
    {
        public override Scope CreateScope(DataGeneratorMetadata dataGeneratorMetadata) => new();

        public override object? Create(Scope scope, Type type) => scope.Resolve();
    }
}
