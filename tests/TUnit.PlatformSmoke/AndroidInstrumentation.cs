#if ANDROID
using Android.App;
using Android.OS;
using Android.Runtime;

namespace TUnit.PlatformSmoke;

[Instrumentation(Name = "net.dot.TUnitSmoke.TestInstrumentation")]
public sealed class AndroidInstrumentation(IntPtr handle, JniHandleOwnership ownership)
    : Instrumentation(handle, ownership)
{
    public override void OnCreate(Bundle? arguments)
    {
        base.OnCreate(arguments);
        Start();
    }

    public override async void OnStart()
    {
        base.OnStart();
        using var result = new Bundle();
        try
        {
            var resultsDirectory = Path.Combine(Application.Context.GetExternalFilesDir(null)!.AbsolutePath, "TestResults");
            // Use the same generated entry point and registered hooks as every other platform.
            var exitCode = await MicrosoftTestingPlatformEntryPoint.Main([
                "--minimum-expected-tests", "1",
                "--report-trx", "--report-trx-filename", "smoke.trx",
                "--results-directory", resultsDirectory
            ]);
            result.PutString("resultsPath", Path.Combine(resultsDirectory, "smoke.trx"));
            if (exitCode != 0)
            {
                result.PutString("error", $"TUnit exited with code {exitCode}.");
            }

            Finish(exitCode == 0 ? Result.Ok : Result.Canceled, result);
        }
        catch (Exception exception)
        {
            result.PutString("error", exception.ToString());
            Finish(Result.Canceled, result);
        }
    }
}
#endif
