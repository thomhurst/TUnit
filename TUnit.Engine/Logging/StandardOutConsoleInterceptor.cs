﻿using Microsoft.Testing.Platform.CommandLine;
using TUnit.Core;

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

namespace TUnit.Engine.Logging;

internal class StandardOutConsoleInterceptor : ConsoleInterceptor
{
    public static StandardOutConsoleInterceptor Instance { get; private set; } = null!;

    public static TextWriter DefaultOut { get; }

    protected override StringWriter RedirectedOut => Context.Current.OutputWriter!;

    static StandardOutConsoleInterceptor()
    {
        DefaultOut = Console.Out;
    }

    public StandardOutConsoleInterceptor(ICommandLineOptions commandLineOptions) : base(commandLineOptions)
    {
        Instance = this;
    }
    
    public void Initialize()
    {
        Console.SetOut(this);
    }

    private protected override TextWriter GetOriginalOut()
    {
        return DefaultOut;
    }

    private protected override void ResetDefault()
    {
        Console.SetOut(DefaultOut);
    }
}