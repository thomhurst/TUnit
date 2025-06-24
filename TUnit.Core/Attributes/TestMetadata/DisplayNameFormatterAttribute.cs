using System.Threading.Tasks;
using TUnit.Core.Contexts;
using TUnit.Core.Interfaces;

#pragma warning disable CS9113 // Parameter is unread - Used for source generator

namespace TUnit.Core;

/// <summary>
/// Base attribute class that provides functionality for formatting display names of tests in TUnit.
/// Concrete implementations of this class can customize how test names are displayed in test results.
/// </summary>
/// <remarks>
/// This attribute can be applied at the method, class, or assembly level to control test name formatting.
/// Subclasses must implement the <see cref="FormatDisplayName"/> method to define custom formatting logic.
/// The attribute interfaces with the test discovery system to set display names for tests during discovery.
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class | AttributeTargets.Assembly, Inherited = false)]
public abstract class DisplayNameFormatterAttribute : TUnitAttribute, ITestDiscoveryEventReceiver
{
    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public ValueTask OnTestDiscovered(TestDiscoveryContext context)
    {
        var displayName = FormatDisplayName(context);

        context.SetDisplayName(displayName);
        return default;
    }

    /// <summary>
    /// When implemented in derived classes, formats the display name for a test.
    /// </summary>
    /// <param name="context">
    /// The test discovery context containing information about the test being discovered.
    /// </param>
    /// <returns>
    /// A string containing the formatted display name for the test.
    /// </returns>
    protected abstract string FormatDisplayName(TestDiscoveryContext context);
}
