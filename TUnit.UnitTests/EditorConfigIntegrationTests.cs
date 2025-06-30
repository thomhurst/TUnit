namespace TUnit.UnitTests;

/// <summary>
/// Tests for EditorConfig integration and configuration validation
/// </summary>
public class EditorConfigIntegrationTests
{
    [Test]
    public async Task EditorConfig_ContainsTUnitConfigurationOptions()
    {
        // Arrange
        var editorConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".editorconfig");
        
        // Act & Assert - Verify .editorconfig file exists and contains TUnit settings
        await Assert.That(File.Exists(editorConfigPath)).IsTrue();
        
        var content = await File.ReadAllTextAsync(editorConfigPath);
        
        // Verify TUnit configuration section exists
        await Assert.That(content).Contains("# TUnit Configuration");
        
        // Verify key configuration options are documented
        await Assert.That(content).Contains("tunit.generic_depth_limit");
        await Assert.That(content).Contains("tunit.aot_only_mode");
        await Assert.That(content).Contains("tunit.enable_property_injection");
        await Assert.That(content).Contains("tunit.enable_valuetask_hooks");
        await Assert.That(content).Contains("tunit.enable_verbose_diagnostics");
        await Assert.That(content).Contains("tunit.max_generic_instantiations");
        await Assert.That(content).Contains("tunit.enable_auto_generic_discovery");
    }

    [Test] 
    public async Task EditorConfig_DocumentsExpectedDefaultValues()
    {
        // Arrange
        var editorConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".editorconfig");
        var content = await File.ReadAllTextAsync(editorConfigPath);
        
        // Act & Assert - Verify documented default values match expected behavior
        await Assert.That(content).Contains("# Default: 5");
        await Assert.That(content).Contains("# Default: false (will become true in future versions)");
        await Assert.That(content).Contains("# Default: true");
        await Assert.That(content).Contains("# Default: 10");
    }

    [Test]
    public async Task EditorConfig_ContainsUsefulDocumentation()
    {
        // Arrange
        var editorConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".editorconfig");
        var content = await File.ReadAllTextAsync(editorConfigPath);
        
        // Act & Assert - Verify documentation provides helpful context
        await Assert.That(content).Contains("Controls the maximum depth for generic type resolution");
        await Assert.That(content).Contains("Enforces AOT-only mode, disabling all reflection fallbacks");
        await Assert.That(content).Contains("Enables dependency injection via property setters");
        await Assert.That(content).Contains("Enables ValueTask return types in hook methods");
        await Assert.That(content).Contains("Enables verbose diagnostic messages from the source generator");
        await Assert.That(content).Contains("Controls the maximum number of generic instantiations per type");
        await Assert.That(content).Contains("Enables automatic discovery of generic test instantiations");
    }

    [Test]
    public async Task EditorConfig_AllOptionsAreCommentedByDefault()
    {
        // Arrange
        var editorConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".editorconfig");
        var content = await File.ReadAllTextAsync(editorConfigPath);
        
        // Act & Assert - Verify all TUnit options are commented out by default
        await Assert.That(content).Contains("# tunit.generic_depth_limit = 5");
        await Assert.That(content).Contains("# tunit.aot_only_mode = true");
        await Assert.That(content).Contains("# tunit.enable_property_injection = true");
        await Assert.That(content).Contains("# tunit.enable_valuetask_hooks = true");
        await Assert.That(content).Contains("# tunit.enable_verbose_diagnostics = false");
        await Assert.That(content).Contains("# tunit.max_generic_instantiations = 10");
        await Assert.That(content).Contains("# tunit.enable_auto_generic_discovery = true");
    }

    [Test]
    public async Task EditorConfig_ContainsReferenceToDocumentation()
    {
        // Arrange
        var editorConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".editorconfig");
        var content = await File.ReadAllTextAsync(editorConfigPath);
        
        // Act & Assert - Verify documentation reference is included
        await Assert.That(content).Contains("For more information about TUnit configuration options");
        await Assert.That(content).Contains("github.com/thomhurst/TUnit");
    }

    [Test]
    public async Task EditorConfig_ValidateStructureAndFormat()
    {
        // Arrange
        var editorConfigPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".editorconfig");
        var content = await File.ReadAllTextAsync(editorConfigPath);
        var lines = content.Split('\n');
        
        // Act & Assert - Verify proper structure
        var tunitConfigStartLine = lines.FirstOrDefault(l => l.Contains("# TUnit Configuration"));
        await Assert.That(tunitConfigStartLine).IsNotNull();
        
        // Verify proper comment formatting
        var tunitConfigLines = lines.SkipWhile(l => !l.Contains("# TUnit Configuration"))
                                   .TakeWhile(l => !l.StartsWith("# Verify") && !string.IsNullOrEmpty(l.Trim()))
                                   .ToList();
        
        await Assert.That(tunitConfigLines).HasCount().GreaterThan(10); // Should have multiple configuration lines
        
        // Verify each config option follows format: "# setting_name = value"
        var configOptionLines = tunitConfigLines.Where(l => l.StartsWith("# tunit.")).ToList();
        await Assert.That(configOptionLines).HasCount().EqualTo(7); // Should have exactly 7 TUnit config options
    }
}