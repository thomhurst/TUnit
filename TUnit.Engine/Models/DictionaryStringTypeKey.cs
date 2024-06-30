namespace TUnit.Engine.Models;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public record DictionaryStringTypeKey(string Key, Type Type)
{
    public virtual bool Equals(DictionaryStringTypeKey? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Key == other.Key && Type == other.Type;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Key, Type);
    }
}