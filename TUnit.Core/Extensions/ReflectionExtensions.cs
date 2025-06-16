using System.Reflection;

namespace TUnit.Core.Extensions;

public static class ReflectionExtensions
{
    internal static string GetFormattedName(this Type type)
    {
        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var genericArguments = string.Join(",", type.GetGenericArguments().Select(GetFormattedName));

        var backtickIndex = type.Name.IndexOf("`", StringComparison.Ordinal);
        if (backtickIndex == -1)
        {
            return $"{type.Name}<{genericArguments}>";
        }

        return $"{type.Name[..backtickIndex]}<{genericArguments}>";
    }

    internal static bool HasExactAttribute<T>(this MemberInfo member)
    {
        return HasAttribute<T>(member, false);
    }

    internal static bool HasAttribute<T>(this MemberInfo member, bool inherit = true)
    {
        try
        {
            if (typeof(T).IsAssignableTo(typeof(Attribute)))
            {
                return member.IsDefined(typeof(T), inherit);
            }

            return member.GetCustomAttributes(inherit)
                .Any(x => x.GetType().IsAssignableTo(typeof(T)));
        }
        catch (NotSupportedException ex) when (ex.Message.Contains("Generic types are not valid"))
        {
            // Fall back to safe method for .NET Framework
            return member.GetCustomAttributesSafe(inherit)
                .Any(x => x.GetType().IsAssignableTo(typeof(T)));
        }
    }

    /// <summary>
    /// Gets custom attributes from a MemberInfo, handling generic attributes on .NET Framework.
    /// </summary>
    /// <param name="member">The member to get attributes from</param>
    /// <param name="inherit">Whether to search the inheritance chain</param>
    /// <returns>An array of attributes</returns>
    /// <remarks>
    /// This method works around the limitation in .NET Framework where GetCustomAttributes
    /// throws NotSupportedException for generic attributes.
    /// </remarks>
    public static Attribute[] GetCustomAttributesSafe(this MemberInfo member, bool inherit = true)
    {
#if NETSTANDARD
        return GetAttributesViaCustomAttributeData(member, typeof(object), inherit);
#else
        return member.GetCustomAttributes(inherit).Cast<Attribute>().ToArray();
#endif
    }

    /// <summary>
    /// Gets custom attributes of a specific type from a MemberInfo, handling generic attributes on .NET Framework.
    /// </summary>
    /// <typeparam name="T">The type of attribute to get</typeparam>
    /// <param name="member">The member to get attributes from</param>
    /// <param name="inherit">Whether to search the inheritance chain</param>
    /// <returns>An array of attributes of the specified type</returns>
    public static T[] GetCustomAttributesSafe<T>(this MemberInfo member, bool inherit = true) where T : Attribute
    {
#if NETSTANDARD
        return GetAttributesViaCustomAttributeData(member, typeof(T), inherit)
                .OfType<T>()
                .ToArray();
#else
        return member.GetCustomAttributes<T>(inherit).ToArray();
#endif
    }

    private static Attribute[] GetAttributesViaCustomAttributeData(MemberInfo member, Type attributeType, bool inherit)
    {
        var attributes = new List<Attribute>();

        var customAttributeDataList = CustomAttributeData.GetCustomAttributes(member)
            .Where(x => inherit
                ? x.AttributeType.IsAssignableTo(attributeType)
                : x.AttributeType == attributeType)
            .ToList();

        foreach (var attributeData in customAttributeDataList)
        {
            try
            {
                var attribute = CreateAttributeInstance(attributeData);

                if (attribute != null)
                {
                    attributes.Add(attribute);
                }
            }
            catch
            {
                // Skip attributes that can't be instantiated
            }
        }

        // If inherit is true and member is a Type, get attributes from base types
        if (inherit && member is Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                attributes.AddRange(GetAttributesViaCustomAttributeData(baseType, type, false));
                baseType = baseType.BaseType;
            }
        }

        return attributes.ToArray();
    }

    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL2072:Target type's member does not satisfy requirements", Justification = "Attribute types are preserved by the runtime")]
    [System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "Attribute instantiation is required for .NET Framework compatibility")]
    private static Attribute? CreateAttributeInstance(CustomAttributeData attributeData)
    {
        var attributeType = attributeData.AttributeType;

        // Skip if it's a generic type definition (not a constructed generic type)
        if (attributeType.IsGenericTypeDefinition)
        {
            return null;
        }

        // Get constructor arguments
        var constructorArgs = attributeData.ConstructorArguments
            .Select(arg => arg.Value)
            .ToArray();

        // Try to create instance
        Attribute? attribute;

        if (constructorArgs.Length == 0)
        {
            // Try parameterless constructor
            attribute = Activator.CreateInstance(attributeType) as Attribute;
        }
        else
        {
            // Try constructor with parameters
            attribute = Activator.CreateInstance(attributeType, constructorArgs) as Attribute;
        }

        if (attribute == null)
        {
            return null;
        }

        foreach (var namedArg in attributeData.NamedArguments ?? [])
        {
            if (namedArg.MemberInfo is PropertyInfo property)
            {
                property.SetValue(attribute, namedArg.TypedValue.Value);
            }
            else if (namedArg.MemberInfo is FieldInfo field)
            {
                field.SetValue(attribute, namedArg.TypedValue.Value);
            }
        }

        return attribute;
    }
}
