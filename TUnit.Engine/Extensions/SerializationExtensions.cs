﻿using System.Text.Json;
using System.Text.Json.Serialization;
using TUnit.Engine.Json;

namespace TUnit.Engine.Extensions;

internal static class SerializationExtensions
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new ObjectArrayConverter() },
        ReferenceHandler = ReferenceHandler.Preserve,
    };
    
    public static string? ToJson<T>(this T? t)
    {
        if (t is null)
        {
            return null;
        }

        return JsonSerializer.Serialize(t, Options);
    }
    
    public static T? FromJson<T>(this string? value)
    {
        if (value is null)
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(value, Options);
    }
    
    public static string? SerializeArgumentsSafely(this object?[]? arguments)
    {
        if (arguments is null)
        {
            return null;
        }
        
        return JsonSerializer.Serialize(arguments, Options);
    }
    
    public static object?[]? DeserializeArgumentsSafely(this string? argumentsJson)
    {
        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<object?[]?>(argumentsJson, Options);
    }

    private static object? FromJsonElement(object? obj, string? typeName)
    {
        if (obj is null)
        {
            return null;
        }
        
        if (obj is JsonElement jsonElement)
        {
            switch (jsonElement.ValueKind)
            {
                case JsonValueKind.String:
                    return jsonElement.GetString();
                case JsonValueKind.Number:
                    return jsonElement.GetInt64();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                case JsonValueKind.Undefined:
                    return null;
                case JsonValueKind.Object:
                    return jsonElement.Deserialize(Type.GetType(typeName!, false) ?? typeof(object));
                case JsonValueKind.Array:
                    if (typeName?.EndsWith("[]") == true)
                    {
                        return jsonElement.EnumerateArray().ToArray();
                    }
                    return jsonElement.EnumerateArray().ToList();
                default:
                    return obj;
            }
        }

        return obj;
    }
}