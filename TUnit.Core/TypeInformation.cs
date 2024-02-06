using System.Reflection;

namespace TUnit.Core;

internal record TypeInformation(Assembly Assembly)
{
    public Type[] Types { get; } = Assembly.GetTypes();
}