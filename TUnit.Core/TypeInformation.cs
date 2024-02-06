using System.Reflection;

namespace TUnit.Core;

internal record TypeInformation(AssemblyWithSource Assembly)
{
    public Type[] Types { get; } = Assembly.Assembly.GetTypes();
}