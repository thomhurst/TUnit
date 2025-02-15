using Microsoft.Extensions.Logging;

namespace TUnit.Templates.Tests;

/// <summary>
/// Base class for template tests. Provides:
/// - an instance of <see cref="ILoggerFactory"/> so test failures are captured by the test harness (and cleans up after the test)
/// - an instance of <see cref="TemplateVerifierOptions"/> that points to the template to install
/// </summary>
/// <remarks>
/// Uses the <see cref="VerificationEngine"/>.
/// See https://github.com/dotnet/templating/blob/main/docs/authoring-tools/Templates-Testing-Tooling.md
/// for usage information.
/// </remarks>
public abstract class TemplateTestBase : IDisposable
{
    private bool _disposed;
    private ILoggerFactory _loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());

    protected abstract string TemplateShortName { get; }

    protected VerificationEngine Engine => new(_loggerFactory);

    protected TemplateVerifierOptions Options => new(TemplateShortName)
    {
        TemplatePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "content", TemplateShortName),
    };

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _loggerFactory.Dispose();
            }

            _disposed = true;
        }
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}