using System.Diagnostics.CodeAnalysis;
using TUnit.Core;
using TUnit.Core.Extensions;
using TUnit.Core.Interfaces;

namespace TUnit.Engine.Services;

[UnconditionalSuppressMessage("Trimming", "IL2075:*")]
internal class ReflectionDataInitializer
{
    private static readonly List<object?> _objects = [];
    private static readonly byte[] _netFrameworkPublicKeyToken = [0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89];
    private static readonly byte[] _netCorePublicKeyTokenPublicKeyToken = [0x7c, 0xec, 0x85, 0xd7, 0xbe, 0xa7, 0x79, 0x8e];
    private static readonly byte[] _systemPrivatePublicKeyToken = [0xcc, 0x7b, 0x13, 0xff, 0xcd, 0x2d, 0xdd, 0x51];
    private static readonly byte[] _mscorlibPublicKeyToken = [0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a];

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

        if (publicKeyToken is not null)
        {
            if (publicKeyToken.SequenceEqual(_mscorlibPublicKeyToken)
                || publicKeyToken.SequenceEqual(_netFrameworkPublicKeyToken)
                || publicKeyToken.SequenceEqual(_netCorePublicKeyTokenPublicKeyToken)
                || publicKeyToken.SequenceEqual(_systemPrivatePublicKeyToken))
            {
                return;
            }
        }

        foreach (var propertyInfo in type.GetProperties())
        {
            if (propertyInfo.GetIndexParameters().Length == 0)
            {
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
