using System.Globalization;

namespace TUnit.Core;

public class CultureExecutor(CultureInfo cultureInfo) : DedicatedThreadExecutor
{
    protected override void ConfigureThread(Thread thread)
    {
        thread.CurrentCulture = cultureInfo;
        thread.CurrentUICulture = cultureInfo;
    }
}
