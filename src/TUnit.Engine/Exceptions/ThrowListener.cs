using System.Diagnostics;
using TUnit.Core.Exceptions;

namespace TUnit.Engine.Exceptions;

internal class ThrowListener : TextWriterTraceListener
{
    public override void Fail(string? message)
    {
        throw new TUnitException(message);
    }

    public override void Fail(string? message, string? detailMessage)
    {
        throw new TUnitException($"{message}{Environment.NewLine}{detailMessage}");
    }
}
