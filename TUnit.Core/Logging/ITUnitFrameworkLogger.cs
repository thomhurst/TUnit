namespace TUnit.Core.Logging;

internal interface ITUnitFrameworkLogger
{
    Task LogInformationAsync(string text);
    Task LogWarningAsync(string text);
    Task LogErrorAsync(string text);
    Task LogErrorAsync(Exception exception);
}