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

    /// <summary>
    /// Checks if an ICustomAttributeProvider has an attribute of the specified type (exact match, no inheritance).
    /// </summary>
    /// <typeparam name="T">The type of attribute to check for</typeparam>
    /// <param name="provider">The attribute provider</param>
    /// <returns>True if the attribute is present, false otherwise</returns>
    internal static bool HasExactAttribute<T>(this ICustomAttributeProvider provider)
    {
        return HasAttribute<T>(provider, false);
    }

    internal static bool HasAttribute<T>(this MemberInfo member, bool inherit = true)
    {
        return ((ICustomAttributeProvider) member).HasAttribute<T>(inherit);
    }

    /// <summary>
    /// Checks if an ICustomAttributeProvider has an attribute of the specified type.
    /// </summary>
    /// <typeparam name="T">The type of attribute to check for</typeparam>
    /// <param name="provider">The attribute provider</param>
    /// <param name="inherit">Whether to search the inheritance chain</param>
    /// <returns>True if the attribute is present, false otherwise</returns>
    internal static bool HasAttribute<T>(this ICustomAttributeProvider provider, bool inherit = true)
    {
        try
        {
            if (typeof(T).IsAssignableTo(typeof(Attribute)) && provider is MemberInfo member)
            {
                return member.IsDefined(typeof(T), inherit);
            }

            return provider.GetCustomAttributes(inherit)
                .Any(x => x.GetType().IsAssignableTo(typeof(T)));
        }
        catch (NotSupportedException ex) when (ex.Message.Contains("Generic types are not valid"))
        {
            // Fall back to safe method for .NET Framework
            return provider.GetCustomAttributesSafe(inherit)
                .Any(x => x.GetType().IsAssignableTo(typeof(T)));
        }
    }

    /// <summary>
    /// Gets custom attributes from an ICustomAttributeProvider, handling generic attributes on .NET Framework.
    /// </summary>
    /// <param name="provider">The attribute provider (MemberInfo, Assembly, ParameterInfo, etc.)</param>
    /// <param name="inherit">Whether to search the inheritance chain</param>
    /// <returns>An array of attributes</returns>
    /// <remarks>
    /// Works around the limitation in .NET Framework where GetCustomAttributes
    /// throws NotSupportedException for generic attributes.
    /// </remarks>
    public static Attribute[] GetCustomAttributesSafe(this ICustomAttributeProvider provider, bool inherit = true)
    {
#if NETSTANDARD
        return GetAttributesViaCustomAttributeData(provider, typeof(Attribute), inherit);
#else
        return provider.GetCustomAttributes(inherit).Cast<Attribute>().ToArray();
#endif
    }

    /// <summary>
    /// Gets custom attributes of a specific type from an ICustomAttributeProvider, handling generic attributes on .NET Framework.
    /// </summary>
    /// <typeparam name="T">The type of attribute to get</typeparam>
    /// <param name="provider">The attribute provider (MemberInfo, Assembly, ParameterInfo, etc.)</param>
    /// <param name="inherit">Whether to search the inheritance chain</param>
    /// <returns>An array of attributes of the specified type</returns>
    public static T[] GetCustomAttributesSafe<T>(this ICustomAttributeProvider provider, bool inherit = true) where T : Attribute
    {
#if NETSTANDARD
        return GetAttributesViaCustomAttributeData(provider, typeof(T), inherit)
                .OfType<T>()
                .ToArray();
#else
        return provider.GetCustomAttributes(typeof(T), inherit).Cast<T>().ToArray();
#endif
    }

    private static Attribute[] GetAttributesViaCustomAttributeData(ICustomAttributeProvider provider, Type attributeType, bool inherit)
    {
        var attributes = new List<Attribute>();

        IList<CustomAttributeData> customAttributeDataList;

        if (provider is MemberInfo memberInfo)
        {
            customAttributeDataList = CustomAttributeData.GetCustomAttributes(memberInfo);
        }
        else if (provider is Assembly assembly)
        {
            customAttributeDataList = CustomAttributeData.GetCustomAttributes(assembly);
        }
        else if (provider is ParameterInfo parameterInfo)
        {
            customAttributeDataList = CustomAttributeData.GetCustomAttributes(parameterInfo);
        }
        else if (provider is Module module)
        {
            customAttributeDataList = CustomAttributeData.GetCustomAttributes(module);
        }
        else
        {
            return [];
        }

        var filteredList = customAttributeDataList
            .Where(x => attributeType == typeof(Attribute) || (inherit
                ? x.AttributeType.IsAssignableTo(attributeType)
                : x.AttributeType == attributeType))
            .ToList();

        foreach (var attributeData in filteredList)
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
            }
        }

        if (inherit && provider is Type type)
        {
            var baseType = type.BaseType;
            while (baseType != null && baseType != typeof(object))
            {
                attributes.AddRange(GetAttributesViaCustomAttributeData(baseType, attributeType, false));
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

        if (attributeType.IsGenericTypeDefinition)
        {
            return null;
        }

        var constructorArgs = attributeData.ConstructorArguments
            .Select(arg => arg.Value)
            .ToArray();

        Attribute? attribute;

        if (constructorArgs.Length == 0)
        {
            attribute = Activator.CreateInstance(attributeType) as Attribute;
        }
        else
        {
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
