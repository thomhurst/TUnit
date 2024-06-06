using Microsoft.Testing.Extensions;
using Microsoft.Testing.Extensions.TrxReport.Abstractions;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Helpers;
using Microsoft.Testing.Platform.Services;
using TUnit.Engine.Properties;

namespace TUnit.Engine;

public static class TestApplicationBuilderExtensions
{
    public static void AddTUnit(this ITestApplicationBuilder testApplicationBuilder)
    {
        TUnitExtension extension = new();
        
        // testApplicationBuilder.CommandLine.AddProvider(() => new FilterCommandProvider(extension));
        
        testApplicationBuilder.RegisterTestFramework(
            serviceProvider => new TestFrameworkCapabilities(new TrxReportProperty()),
            (capabilities, serviceProvider) => new TUnitTestFramework(extension, serviceProvider, capabilities));
            
        testApplicationBuilder.AddTreeNodeFilterService(extension);
    }
}