﻿using System.Reflection;

namespace TUnit.Engine.Helpers;

#if !DEBUG
using System.ComponentModel;
[EditorBrowsable(EditorBrowsableState.Never)]
#endif
public static class AttributeHelper
{
    public static TAttribute? GetAttribute<TAttribute>(Type type, MethodInfo methodInfo) where TAttribute : Attribute
    {
        return methodInfo.GetCustomAttributes<TAttribute>()
            .Concat(type.GetCustomAttributes<TAttribute>())
            .FirstOrDefault();
    }
    
    public static TAttribute? GetAttribute<TAttribute>(IEnumerable<Attribute> attributes) where TAttribute : Attribute
    {
        return attributes
            .OfType<TAttribute>()
            .FirstOrDefault();
    }
}