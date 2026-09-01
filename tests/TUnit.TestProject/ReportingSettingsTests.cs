namespace TUnit.TestProject;

public static class ReportingSettingsHooks
{
    internal const string DisableReportingEnvironmentVariable = "TUNIT_TEST_DISABLE_REPORTING_FROM_DISCOVERY_HOOK";

    [Before(TestDiscovery)]
    public static void ConfigureReporting(BeforeTestDiscoveryContext context)
    {
        if (Environment.GetEnvironmentVariable(DisableReportingEnvironmentVariable) is not "true")
        {
            return;
        }

        context.Settings.Reporting.HtmlReportEnabled = false;
        context.Settings.Reporting.JsonReportEnabled = false;
        context.Settings.Reporting.ArtifactUploadEnabled = false;
    }
}

public class ReportingSettingsTests
{
    [Test]
    public void Test() { }
}
