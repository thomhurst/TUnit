using Microsoft.CodeAnalysis;
using TUnit.Core.SourceGenerator.Configuration;

namespace TUnit.Core.SourceGenerator.Tests;

internal class TUnitConfigurationTests
{
    [Test]
    public void TUnitConfiguration_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = TUnitConfiguration.Default;
        
        // Assert
        Assert.That(config.GenericDepthLimit).IsEqualTo(5);
        Assert.That(config.UseStronglyTypedDelegates).IsTrue();
        Assert.That(config.EnablePropertyInjection).IsTrue();
        Assert.That(config.EnableDiagnostics).IsFalse();
        Assert.That(config.UseModuleInitializer).IsTrue();
        Assert.That(config.EnableBoxingElimination).IsTrue();
    }

    [Test]
    public void TUnitConfiguration_CreateFromOptions_ParsesValues()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["tunit.generic_depth_limit"] = "10",
            ["tunit.use_strongly_typed_delegates"] = "false",
            ["tunit.enable_property_injection"] = "false",
            ["tunit.enable_diagnostics"] = "true",
            ["tunit.use_module_initializer"] = "false",
            ["tunit.enable_boxing_elimination"] = "false"
        });
        
        // Act
        var config = TUnitConfiguration.Create(options);
        
        // Assert
        Assert.That(config.GenericDepthLimit).IsEqualTo(10);
        Assert.That(config.UseStronglyTypedDelegates).IsFalse();
        Assert.That(config.EnablePropertyInjection).IsFalse();
        Assert.That(config.EnableDiagnostics).IsTrue();
        Assert.That(config.UseModuleInitializer).IsFalse();
        Assert.That(config.EnableBoxingElimination).IsFalse();
    }

    [Test]
    public void TUnitConfiguration_InvalidValues_UsesDefaults()
    {
        // Arrange
        var options = new MockAnalyzerConfigOptions(new Dictionary<string, string>
        {
            ["tunit.generic_depth_limit"] = "invalid",
            ["tunit.use_strongly_typed_delegates"] = "invalid",
            ["tunit.enable_property_injection"] = "invalid"
        });
        
        // Act
        var config = TUnitConfiguration.Create(options);
        
        // Assert - should use defaults for invalid values
        Assert.That(config.GenericDepthLimit).IsEqualTo(5);
        Assert.That(config.UseStronglyTypedDelegates).IsTrue();
        Assert.That(config.EnablePropertyInjection).IsTrue();
    }

    private class MockAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options;

        public MockAnalyzerConfigOptions(Dictionary<string, string> options)
        {
            _options = options;
        }

        public override bool TryGetValue(string key, out string value)
        {
            return _options.TryGetValue(key, out value!);
        }
    }
}