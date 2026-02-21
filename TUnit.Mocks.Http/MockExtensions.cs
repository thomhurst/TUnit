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
        /// Creates a new <see cref="HttpClient"/> backed by a <see cref="Http.MockHttpHandler"/>.
        /// Returns both so the handler can be configured and the client injected.
        /// </summary>
        public static (Http.MockHttpHandler Handler, HttpClient Client) HttpClient()
        {
            var handler = new Http.MockHttpHandler();
            return (handler, handler.CreateClient());
        }

        /// <summary>
        /// Creates a new <see cref="HttpClient"/> with a base address, backed by a <see cref="Http.MockHttpHandler"/>.
        /// Returns both so the handler can be configured and the client injected.
        /// </summary>
        public static (Http.MockHttpHandler Handler, HttpClient Client) HttpClient(string baseAddress)
        {
            var handler = new Http.MockHttpHandler();
            return (handler, handler.CreateClient(baseAddress));
        }
    }
}
