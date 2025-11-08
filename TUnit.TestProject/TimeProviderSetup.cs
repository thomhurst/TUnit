using System.Runtime.CompilerServices;
using Microsoft.Extensions.Time.Testing;
using TUnit.Core;

namespace TUnit.TestProject;

internal static class TimeProviderSetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        TimeProviderContext.Current = new FakeTimeProvider();
    }
}
