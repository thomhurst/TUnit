using Microsoft.Testing.Platform.Services;

#pragma warning disable TPEXP // Experimental API - GetClientInfo

namespace TUnit.Engine.Services;

internal static class ClientInfoExtensions
{
    /// <summary>
    /// Whether the test host was started by a console client (<c>dotnet run</c>, <c>dotnet test</c>),
    /// as opposed to an IDE or another server-mode client. Console hosts register the
    /// <c>testingplatform-console</c> client id; server-mode hosts register whatever the client sent.
    /// Defaults to console when the client info is unavailable.
    /// </summary>
    public static bool IsConsoleClient(this IServiceProvider serviceProvider)
    {
        try
        {
            return serviceProvider.GetClientInfo().Id.Contains("console", StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to determine console environment: {ex}");
            return true;
        }
    }
}
