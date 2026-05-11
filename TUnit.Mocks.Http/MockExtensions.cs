namespace TUnit.Mocks;

/// <summary>
/// Extends <see cref="Mock"/> with HTTP mocking factory methods.
/// Available when the <c>TUnit.Mocks.Http</c> package is referenced.
/// </summary>
public static class MockExtensions
{
    extension(Mock)
    {
        /// <summary>
        /// Creates a new <see cref="Http.MockHttpHandler"/> for mocking HTTP client interactions.
        /// </summary>
        public static Http.MockHttpHandler HttpHandler() => new();

        /// <summary>
        /// Creates a new <see cref="Http.MockHttpClient"/> backed by a <see cref="Http.MockHttpHandler"/>.
        /// Use <c>.Handler</c> on the returned client to configure setups and verify calls.
        /// </summary>
        public static Http.MockHttpClient HttpClient() => new();

        /// <summary>
        /// Creates a new <see cref="Http.MockHttpClient"/> with a base address.
        /// Use <c>.Handler</c> on the returned client to configure setups and verify calls.
        /// </summary>
        public static Http.MockHttpClient HttpClient(string baseAddress) => new(baseAddress);

        /// <summary>
        /// Creates a mock <see cref="IHttpClientFactory"/> backed by a fresh <see cref="Http.MockHttpHandler"/>.
        /// Each <c>CreateClient</c> call returns a new <see cref="System.Net.Http.HttpClient"/> sharing the
        /// same handler, with handler disposal disabled — safe to use in <c>using</c> blocks.
        /// </summary>
        public static Http.MockHttpClientFactory HttpClientFactory() => new();

        /// <summary>
        /// Creates a mock <see cref="IHttpClientFactory"/> backed by the supplied default handler.
        /// </summary>
        public static Http.MockHttpClientFactory HttpClientFactory(Http.MockHttpHandler handler) => new(handler);
    }
}
