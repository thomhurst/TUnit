using System.Security.Cryptography;
using System.Text;

namespace TUnit.Engine.Services;

internal static class TestIdGenerator
{
    public static string GenerateTestId(
        Type testClass,
        string methodName,
        object?[] classArgs,
        object?[] methodArgs,
        int repeatAttempt)
    {
        var sb = new StringBuilder();
        sb.Append(testClass.FullName);
        sb.Append('.');
        sb.Append(methodName);
        sb.Append('(');
        
        // Add class arguments
        if (classArgs.Length > 0)
        {
            sb.Append("class:");
            AppendArguments(sb, classArgs);
            sb.Append(';');
        }
        
        // Add method arguments
        if (methodArgs.Length > 0)
        {
            sb.Append("method:");
            AppendArguments(sb, methodArgs);
            sb.Append(';');
        }
        
        // Add repeat attempt
        if (repeatAttempt > 0)
        {
            sb.Append("repeat:");
            sb.Append(repeatAttempt);
        }
        
        sb.Append(')');
        
        // Generate a hash for long IDs
        var testIdBase = sb.ToString();
        if (testIdBase.Length > 200)
        {
            var hash = ComputeHash(testIdBase);
            return $"{testClass.FullName}.{methodName}_{hash}";
        }
        
        return testIdBase;
    }
    
    private static void AppendArguments(StringBuilder sb, object?[] args)
    {
        for (var i = 0; i < args.Length; i++)
        {
            if (i > 0)
            {
                sb.Append(',');
            }
            sb.Append(args[i]?.ToString() ?? "null");
        }
    }
    
    private static string ComputeHash(string input)
    {
        var inputBytes = Encoding.UTF8.GetBytes(input);
#if NET6_0_OR_GREATER
        var hashBytes = SHA256.HashData(inputBytes);
#else
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(inputBytes);
#endif
        return Convert.ToBase64String(hashBytes).Substring(0, 8).Replace('/', '_').Replace('+', '-');
    }
}