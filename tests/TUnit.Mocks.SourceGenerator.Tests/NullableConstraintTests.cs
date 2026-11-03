namespace TUnit.Mocks.SourceGenerator.Tests;

public class NullableConstraintTests : SnapshotTestBase
{
    [Test]
    public Task Generic_Constraints_Preserve_Nullability()
    {
        var source = """
            #nullable enable
            using System.Threading.Tasks;
            using TUnit.Mocks;

            public class Foo { }
            public interface IService
            {
                Task<T> Get<T>(string key) where T : class?;
                T Base<T>() where T : Foo?;
                T Required<T>() where T : notnull;
                T? Value<T>() where T : struct;
                T Unmanaged<T>() where T : unmanaged;
            }
            public class Usage
            {
                void Setup()
                {
                    var mock = Mock.Of<IService>();
                }
            }
            """;

        AssertGeneratedCodeHasNoNullableWarnings(source);
        return VerifyGeneratorOutput(source);
    }
}
