using TUnit.Core.Converters;

namespace TUnit.TestProject;

public class GlobalSetup
{
    [Before(TestDiscovery)]
    public static void SetupAotConverters()
    {
        AotConverterRegistry.Register<string, MixedMatrixTestsUnion1>(value => new MixedMatrixTestsUnion1(value));
        AotConverterRegistry.Register<MixedMatrixTests.Enum4, MixedMatrixTestsUnion1>(value => new MixedMatrixTestsUnion1(value));
        AotConverterRegistry.Register<MixedMatrixTests.Enum5, MixedMatrixTestsUnion1>(value => new MixedMatrixTestsUnion1(value));

        AotConverterRegistry.Register<string, MixedMatrixTestsUnion2>(value => new MixedMatrixTestsUnion2(value));
        AotConverterRegistry.Register<MixedMatrixTests.Enum4, MixedMatrixTestsUnion2>(value => new MixedMatrixTestsUnion2(value));
        AotConverterRegistry.Register<MixedMatrixTests.Enum5, MixedMatrixTestsUnion2>(value => new MixedMatrixTestsUnion2(value));
    }
}
