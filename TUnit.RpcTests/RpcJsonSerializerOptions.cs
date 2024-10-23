using System.Text.Json;

namespace TUnit.RpcTests;

public static class RpcJsonSerializerOptions
{
    public static JsonSerializerOptions Default { get; }

    static RpcJsonSerializerOptions()
    {
        Default = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
        };
    }
}