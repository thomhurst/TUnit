using TUnit.Aspire;

namespace ExampleNamespace.TestProject;

public class AppFixture : AspireFixture<Projects.ExampleNamespace_AppHost>
{
    protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
    {
        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
        {
            clientBuilder.AddStandardResilienceHandler();
        });
    }
}
