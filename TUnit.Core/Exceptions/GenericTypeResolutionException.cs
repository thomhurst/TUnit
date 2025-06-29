using System;

namespace TUnit.Core.Exceptions;

/// <summary>
/// Exception thrown when generic type resolution fails
/// </summary>
public class GenericTypeResolutionException : Exception
{
    public GenericTypeResolutionException(string message) : base(message)
    {
    }

    public GenericTypeResolutionException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
