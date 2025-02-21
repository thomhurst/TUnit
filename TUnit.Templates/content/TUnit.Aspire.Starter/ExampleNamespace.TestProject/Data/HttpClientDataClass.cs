using TUnit.Core.Interfaces;

namespace ExampleNamespace.TestProject.Data
{
    public class HttpClientDataClass: IAsyncInitializer, IAsyncDisposable
    {
        public HttpClient HttpClient { get; private set; } = new();
        public async Task InitializeAsync()
        {
            HttpClient = (GlobalHooks.App ?? throw new NullReferenceException()).CreateHttpClient("apiservice");
            if (GlobalHooks.NotificationService != null)
            {
                await GlobalHooks.NotificationService.WaitForResourceAsync("apiservice", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
            }
        }

        public async ValueTask DisposeAsync()
        {
            await Console.Out.WriteLineAsync("And when the class is finished with, we can clean up any resources.");
        }
    }
}
