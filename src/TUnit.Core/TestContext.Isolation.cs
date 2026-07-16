using TUnit.Core.Interfaces;

namespace TUnit.Core;

public partial class TestContext
{
    internal static int _isolationIdCounter;

    internal int IsolationUniqueId { get; }

    /// <inheritdoc/>
    int ITestIsolation.UniqueId => IsolationUniqueId;

    /// <inheritdoc/>
    string ITestIsolation.GetIsolatedName(string baseName) => $"Test_{IsolationUniqueId}_{baseName}";

    /// <inheritdoc/>
    string ITestIsolation.GetIsolatedPrefix(string separator) => $"test{separator}{IsolationUniqueId}{separator}";
}
