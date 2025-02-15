using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

internal class StandardErrorConsoleInterceptor : ConsoleInterceptor
{
    public static StandardErrorConsoleInterceptor Instance { get; private set; } = null!;

    public static TextWriter DefaultError { get; }

    protected override TextWriter? RedirectedOut => Context.Current.ErrorOutputWriter;
    
    static StandardErrorConsoleInterceptor()
    {
        DefaultError = Console.Error;
    }

    public StandardErrorConsoleInterceptor(ICommandLineOptions commandLineOptions) : base(commandLineOptions)
    {
        Instance = this;
    }
    
    public void Initialize()
    {
        Console.SetError(this);
    }

    private protected override TextWriter GetOriginalOut()
    {
        return DefaultError;
    }

    private protected override void ResetDefault()
    {
        Console.SetError(DefaultError);
    }
}