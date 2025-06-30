namespace TUnit.Core;

public record GlobalSharedDataKey(Type Type) : SharedDataKey(RandomKey.ToString(), Type)
{
    public static readonly Guid RandomKey = Guid.NewGuid();
}
