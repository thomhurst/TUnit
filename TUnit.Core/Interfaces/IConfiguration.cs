namespace TUnit.Core.Interfaces;

/// <summary>
/// Defines an interface for accessing configuration values within the TUnit testing framework.
/// </summary>
/// <remarks>
/// <para>
/// The configuration system provides a way to influence test execution through external settings.
/// </para>
/// <para>
/// Configuration values can come from various sources, including:
/// <list type="bullet">
///   <item><description>Command-line arguments passed to the test runner</description></item>
///   <item><description>Environment variables</description></item>
///   <item><description>Configuration files (like appsettings.json)</description></item>
/// </list>
/// </para>
/// <para>
/// This interface is primarily accessed through the static <see cref="TestContext.Configuration"/> property,
/// making configuration values available to tests without requiring dependency injection.
/// </para>
/// <para>
/// Configuration keys can include nested paths using a colon separator (e.g., "Section:Key").
/// </para>
/// </remarks>
/// <example>
/// <code>
/// [Test]
/// public async Task ConfigurationExample()
/// {
///     // Access a configuration value
///     string? logLevel = TestContext.Configuration.Get("LogLevel");
///     
///     // Use a nested configuration path
///     string? apiUrl = TestContext.Configuration.Get("Services:ApiService:Url");
///     
///     // Check if a feature flag is enabled
///     bool isFeatureEnabled = TestContext.Configuration.Get("FeatureFlags:NewFeature") == "true";
/// }
/// </code>
/// </example>
public interface IConfiguration
{
    /// <summary>
    /// Retrieves a configuration value from the test execution environment by key.
    /// </summary>
    /// <param name="key">The configuration key to look up.</param>
    /// <returns>
    /// The configuration value associated with the specified key, or <c>null</c> if the key is not found.
    /// </returns>
    /// <remarks>
    /// This method provides access to configuration values that can be set via command-line arguments,
    /// environment variables, or configuration files used in the test execution context.
    /// The configuration system is used by the TUnit testing framework to provide runtime
    /// configuration to tests and test infrastructure components.
    /// </remarks>
    /// <example>
    /// <code>
    /// // Accessing a configuration value in a test
    /// string? logLevel = TestContext.Configuration.Get("LogLevel");
    /// if (logLevel == "Verbose")
    /// {
    ///     // Enable verbose logging for this test
    /// }
    /// </code>
    /// </example>
    string? Get(string key);
}