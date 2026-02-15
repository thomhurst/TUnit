using TUnit.Aspire;

namespace TUnit.Aspire.Test;

// Instructions:
// 1. Add a project reference to the target AppHost project, e.g.:
//
//    <ItemGroup>
//        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
//    </ItemGroup>
//
// 2. Create a fixture class for your AppHost (or use AspireFixture directly):
//
//    public class AppFixture : AspireFixture<Projects.MyAspireApp_AppHost>
//    {
//        protected override void ConfigureBuilder(IDistributedApplicationTestingBuilder builder)
//        {
//            builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
//            {
//                clientBuilder.AddStandardResilienceHandler();
//            });
//        }
//    }
//
// 3. Uncomment the following example test and update 'AppFixture' to match your fixture:
//
//[ClassDataSource<AppFixture>(Shared = SharedType.PerTestSession)]
//public class IntegrationTest1(AppFixture fixture)
//{
//    [Test]
//    public async Task GetWebResourceRootReturnsOkStatusCode()
//    {
//        // Arrange
//        var httpClient = fixture.CreateHttpClient("webfrontend");
//        // Act
//        var response = await httpClient.GetAsync("/");
//        // Assert
//        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
//    }
//}
public class IntegrationTest1
{
}
