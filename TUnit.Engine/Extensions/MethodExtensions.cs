using System.Reflection;
using TUnit.Core;

namespace TUnit.Engine.Extensions;

internal static class MethodExtensions
{
    public static TimeSpan? GetTimeout(this MethodInfo methodInfo)
    {
        return methodInfo.GetCustomAttributes().OfType<TimeoutAttribute>().FirstOrDefault()?.Timeout;
    }
}