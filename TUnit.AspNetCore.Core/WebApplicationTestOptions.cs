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
    /// Gets or sets a value indicating whether outbound HTTP calls made by the SUT through
    /// <see cref="System.Net.Http.IHttpClientFactory"/> (including <c>AddHttpClient&lt;T&gt;()</c>,
    /// named clients, and typed clients) should automatically carry the test's
    /// <c>traceparent</c>, <c>baggage</c>, and <c>X-TUnit-TestId</c> headers.
    /// Default is <c>true</c>.
    /// <para>
    /// Set to <c>false</c> when the SUT already instruments its outbound HTTP calls
    /// (for example via the OpenTelemetry HttpClient instrumentation) and you do not want
    /// TUnit to prepend its handlers to every factory pipeline.
    /// </para>
    /// </summary>
    public bool AutoPropagateHttpClientFactory { get; set; } = true;
}
