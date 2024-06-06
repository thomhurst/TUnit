using System;

namespace TUnit.Analyzers.Extensions;

public static class TypeExtensions
{
    public static string GetFullNameWithoutGenericArity(this Type type)
    {
        var name = type.FullName;
        
        var index = name.IndexOf('`');
        
        return index == -1 ? name : name.Substring(0, index);
    }
}