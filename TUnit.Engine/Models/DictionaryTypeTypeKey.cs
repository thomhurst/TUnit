﻿namespace TUnit.Engine.Models;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public record DictionaryTypeTypeKey(Type Type1, Type Type2)
{
    public virtual bool Equals(DictionaryTypeTypeKey? other)
    {
        if (ReferenceEquals(null, other))
        {
            return false;
        }

        if (ReferenceEquals(this, other))
        {
            return true;
        }

        return Type1 == other.Type1 && Type2 == other.Type2;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Type1, Type2);
    }
}