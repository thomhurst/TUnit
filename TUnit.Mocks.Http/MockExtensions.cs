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
    }
}
