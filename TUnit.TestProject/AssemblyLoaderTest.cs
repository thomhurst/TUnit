namespace TUnit.TestProject;

public class AssemblyLoaderTest
{
    [Test]
    public void Test()
    {
        _ = typeof(Confluent.SchemaRegistry.Serdes.Protobuf.Decimal);
        _ = typeof(global::Google.Protobuf.Reflection.CustomOptions);
        _ = typeof(WireMock.Net.Abstractions.FluentBuilder.Builder<>);
    }
}