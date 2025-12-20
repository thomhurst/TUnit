namespace TUnit.AspNetCore;

public record WebApplicationTestOptions
{
    /// <summary>
    /// Gets or sets a value indicating whether HTTP exchange capture is enabled for the test.
    /// When enabled, all HTTP requests and responses are recorded and can be inspected via <see cref="WebApplicationTest{TFactory, TEntryPoint}.HttpCapture"/>.
    /// Default is false.
    /// </summary>
    public bool EnableHttpExchangeCapture { get; set; } = false;
    
    /// <summary>
    /// Gets or sets a value indicating whether TUnit logging is added to the test's service collection.
    /// When enabled, logs from the application under test are captured and written to the test output.
    /// Default is true.
    /// </summary>
    public bool AddTUnitLogging { get; set; } = true;
}
