using System.Text;
using TUnit.Core;

namespace TUnit.Engine;

public class ConsoleInterceptor : TextWriter
{
    public ConsoleInterceptor()
    {
        Encoding = Encoding.UTF8;
        DefaultOut = Console.Out;
        Console.SetOut(this);
    }

    public override void Write(char value)
    {
        TestContext.Current.Write(value);
        base.Write(value);
    }

    public override Task WriteAsync(char value)
    {
        TestContext.Current.Write(value);
        return base.WriteAsync(value);
    }

    public override async ValueTask DisposeAsync()
    {
        Console.SetOut(DefaultOut);
        await base.DisposeAsync();
    }

    public TextWriter DefaultOut { get; set; }
    
    public override Encoding Encoding { get; }
}