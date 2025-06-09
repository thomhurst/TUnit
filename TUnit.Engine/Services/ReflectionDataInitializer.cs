using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

[UnconditionalSuppressMessage("Trimming", "IL2075:*")]
internal class ReflectionDataInitializer
{
    private static readonly List<object?> _objects = [];

    public async Task Initialize(TestContext testContext)
    {
        try
        {
            foreach (var possibleEventObject in testContext.GetPossibleEventObjects())
            {
                await Initialize(possibleEventObject, []);
            }
        }
        catch
        {
            // Ignored
        }
    }

    public async Task Initialize(object? possibleEventObject, HashSet<Type> visited)
    {
        if (possibleEventObject is null)
        {
            return;
        }

        if(!visited.Add(possibleEventObject.GetType()))
        {
            return;
        }

        var type = possibleEventObject.GetType();

        var publicKeyToken = type.Assembly.GetName().GetPublicKeyToken();

        if (Helpers.DotNetAssemblyHelper.IsDotNetCoreLibrary(publicKeyToken))
        {
            return;
        }

        foreach (var propertyInfo in type.GetProperties().Where(p => p.GetMethod != null))
        {
            if (propertyInfo.GetIndexParameters().Length != 0)
            {
                continue;
            }

            try
            {
                var value = propertyInfo.GetValue(possibleEventObject);
                await Initialize(value, visited);
            }
            catch
            {
                // Ignore
            }
        }

        if (possibleEventObject is IAsyncInitializer asyncInitializer)
        {
            await ObjectInitializer.InitializeAsync(asyncInitializer);
        }
    }

    public void RegisterForInitialize(object? value)
    {
        _objects.Add(value);
    }

    public async Task InitializePending()
    {
        foreach (var o in _objects)
        {
            await Initialize(o, []);
        }

        _objects.Clear();
    }
}
