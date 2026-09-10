using TUnit.Aspire;

namespace TUnit.Aspire.Starter.TestProject;

public class AppFixture : AspireFixture<Projects.TUnit.Aspire.Starter_AppHost>
{
    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
    }
}
