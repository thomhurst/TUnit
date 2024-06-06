using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Helpers;

namespace TUnit.Engine;

public static class TestApplicationBuilderExtensions
{
    public static void AddTUnit(this ITestApplicationBuilder testApplicationBuilder)
    {
        TUnitExtension extension = new();
        
        // testApplicationBuilder.CommandLine.AddProvider(() => new FilterCommandProvider(extension));
        
        testApplicationBuilder.RegisterTestFramework(
            _ => new TestFrameworkCapabilities(),
            (capabilities, serviceProvider) => new TUnitTestFramework(extension, serviceProvider, capabilities));
            
        testApplicationBuilder.AddTreeNodeFilterService(extension);
    }
}