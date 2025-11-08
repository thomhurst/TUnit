using System.Runtime.CompilerServices;
using Microsoft.Extensions.Time.Testing;
using TUnit.Core;

namespace TUnit.Assertions.Tests;

internal static class TimeProviderSetup
{
    [ModuleInitializer]
    internal static void Initialize()
    {
        TimeProviderContext.Current = new FakeTimeProvider();
    }
}
