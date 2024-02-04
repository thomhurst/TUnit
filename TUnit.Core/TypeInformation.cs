using System.Reflection;

namespace TUnit.Core;

public record TypeInformation(Assembly Assembly)
{
    public Type[] Types { get; } = Assembly.GetTypes();
}