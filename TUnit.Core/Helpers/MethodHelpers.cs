using System.Reflection;

namespace TUnit.Core.Helpers;

public static class MethodHelpers
{
    public static MethodInfo GetMethodInfo(Delegate @delegate)
    {
        return @delegate.Method;
    }
}