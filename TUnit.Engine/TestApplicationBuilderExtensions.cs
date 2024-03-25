using System.Reflection;
using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Capabilities.TestFramework;
using Microsoft.Testing.Platform.Extensions.CommandLine;

namespace TUnit.Engine;

public static class TestApplicationBuilderExtensions
{
    public static void AddTUnit(this ITestApplicationBuilder testApplicationBuilder, Func<IEnumerable<Assembly>> getTestAssemblies)
    {
        TUnitExtension extension = new();
        
        // testApplicationBuilder.CommandLine.AddProvider(() => new FilterCommandProvider(extension));
        
        testApplicationBuilder.RegisterTestFramework(
            _ => new TestFrameworkCapabilities(),
            (capabilities, serviceProvider) => new TUnitTestFramework(extension, getTestAssemblies, serviceProvider, capabilities));
    }
}