using TUnit.TestProject.Attributes;

namespace TUnit.TestProject;

[PropertyRegistrationDelegateTests.ConstructorData]
public class PropertyRegistrationDelegateTests(int value)
{
    [PropertyData]
    public required Service Injected { get; init; }

    public sealed class Service;

    public sealed class ConstructorDataAttribute : DataSourceGeneratorAttribute<int>
    {
        protected override IEnumerable<Func<int>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            yield return () =>
            {
                metadata.TestBuilderContext.Current.Events.OnTestRegistered += Record("constructor");
                return 1;
            };
        }
    }

    public sealed class PropertyDataAttribute : DataSourceGeneratorAttribute<Service>
    {
        protected override IEnumerable<Func<Service>> GenerateDataSources(DataGeneratorMetadata metadata)
        {
            yield return () =>
            {
                var events = metadata.TestBuilderContext.Current.Events;
                events.OnTestRegistered ??= new();
                // A newly inserted handler sorts ahead of the previously dispatched subscription.
                events.OnTestRegistered.Add(Record("property"), order: 0);
                return new Service();
            };
        }
    }

    private static Func<object, TestContext, ValueTask> Record(string key) => (_, context) =>
    {
        context.StateBag.Items.TryGetValue(key, out var count);
        context.StateBag.Items[key] = (count as int? ?? 0) + 1;
        return default;
    };

    [Test, Repeat(1), EngineTest(ExpectedResult.Pass)]
    public async Task PropertyAndConstructorDelegatesRunOnce()
    {
        await Assert.That(value).IsEqualTo(1);
        await Assert.That(Injected).IsNotNull();
        await Assert.That(TestContext.Current!.StateBag.Items["constructor"]).IsEqualTo(1);
        await Assert.That(TestContext.Current.StateBag.Items["property"]).IsEqualTo(1);
    }
}
