using TUnit.Core.Interfaces;
using TUnit.TestProject.Attributes;

namespace TUnit.TestProject.Bugs._7076;

[EngineTest(ExpectedResult.Pass)]
public class ArgumentsRowSkipTests
{
    [Test]
    [Arguments(1, DisplayName = "custom name for 1")]
    [Arguments(2)]
    public async Task DisplayNameOnlyAppliesToItsOwnRow(int value)
    {
        var displayName = TestContext.Current!.Metadata.DisplayName;

        if (value == 1)
        {
            await Assert.That(displayName).IsEqualTo("custom name for 1");
        }
        else
        {
            await Assert.That(displayName).IsEqualTo("DisplayNameOnlyAppliesToItsOwnRow(2)");
        }
    }
}

[EngineTest(ExpectedResult.Pass)]
[Arguments("a")]
[Arguments("b", Skip = "skip only b")]
public class ClassArgumentsRowSkipTests(string value)
{
    [Test]
    public async Task OnlyMarkedClassRowIsSkipped()
    {
        await Assert.That(value).IsEqualTo("a");
    }
}

[EngineTest(ExpectedResult.Pass)]
[InheritsTests]
public class InheritedArgumentsRowSkipTests : InheritedArgumentsRowSkipBase;

public abstract class InheritedArgumentsRowSkipBase
{
    [Test]
    [Arguments(1)]
    [Arguments(2, Skip = "skip only 2")]
    public async Task OnlyMarkedInheritedRowIsSkipped(int value)
    {
        await Assert.That(value).IsEqualTo(1);
    }
}

[EngineTest(ExpectedResult.Pass)]
[ClassConstructor<ClassConstructorDataSourceProbe>]
[ClassDataSource<ClassConstructorFixture>]
public class ClassConstructorDataSourceTest(ClassConstructorFixture fixture)
{
    [Test]
    [Arguments(42)]
    public async Task MethodDataSourceIsPassedToClassConstructor(int value)
    {
        await Assert.That(value).IsEqualTo(42);
    }
}

public sealed class ClassConstructorDataSourceProbe : IClassConstructor
{
    public Task<object> Create(Type type, ClassConstructorMetadata classConstructorMetadata)
    {
        if (type == typeof(ClassConstructorDataSourceTest) &&
            classConstructorMetadata.TestBuilderContext.DataSourceAttribute is not ArgumentsAttribute)
        {
            throw new InvalidOperationException("The class constructor did not receive the method data source.");
        }

        return Task.FromResult<object>(Activator.CreateInstance(type, [new ClassConstructorFixture()])!);
    }
}

public sealed class ClassConstructorFixture;
