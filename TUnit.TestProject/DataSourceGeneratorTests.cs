using AutoFixture.Kernel;

namespace TUnit.TestProject;

public class DataSourceGeneratorTests
{
    [Test]
    [AutoFixtureGenerator]
    public void DataSource_Method(int value)
    {
        // Dummy method
    }
    
    [Test]
    [AutoFixtureGenerator]
    public void DataSource_Method2(int value)
    {
        // Dummy method
    }

    public static int SomeMethod() => 1;

    public class AutoFixtureGeneratorAttribute : DataSourceGeneratorAttribute
    {
        public override IEnumerable<object[]> GenerateDataSources(Type[] parameterTypes)
        {
            var fixture = new AutoFixture.Fixture();
            yield return parameterTypes.Select(t => fixture.Create(t, new SpecimenContext(fixture))).ToArray();
        }
    }
}