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

    /// <summary>
    /// Gets or sets a value indicating whether the SUT's <see cref="OpenTelemetry.Trace.TracerProvider"/>
    /// should be automatically augmented with the TUnit HTTP activity source, the
    /// <c>TUnitTestCorrelationProcessor</c>, and ASP.NET Core + HttpClient instrumentation.
    /// Default is <c>true</c>.
    /// <para>
    /// When enabled, test spans emitted inside the SUT are tagged with the ambient
    /// <c>tunit.test.id</c> baggage so they remain queryable per-test in backends like
    /// Seq or Jaeger, even when third-party libraries break the parent-chain.
    /// </para>
    /// <para>
    /// Set to <c>false</c> to leave the SUT's OpenTelemetry configuration untouched —
    /// useful if the SUT configures its own processors and you do not want TUnit's
    /// defaults layered on top.
    /// </para>
    /// </summary>
    public bool AutoConfigureOpenTelemetry { get; set; } = true;
}
