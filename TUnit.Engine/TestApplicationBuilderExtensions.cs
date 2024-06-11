using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Helpers;
using TUnit.Engine.Properties;

namespace TUnit.Engine;

public static class TestApplicationBuilderExtensions
{
    public static void AddTUnit(this ITestApplicationBuilder testApplicationBuilder)
    {
        TUnitExtension extension = new();
        
        testApplicationBuilder.RegisterTestFramework(
            serviceProvider => new TestFrameworkCapabilities(new TrxReportCapability()),
            (capabilities, serviceProvider) => new TUnitTestFramework(extension, serviceProvider, capabilities));

        testApplicationBuilder.AddTreeNodeFilterService(extension);
        testApplicationBuilder.CommandLine.AddProvider(() => new JsonOutputCommandProvider(extension));
    }
}