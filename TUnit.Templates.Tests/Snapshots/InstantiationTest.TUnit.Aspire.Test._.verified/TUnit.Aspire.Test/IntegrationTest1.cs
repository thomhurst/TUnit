using TUnit.Aspire.Test.Data;

namespace TUnit.Aspire.Test
{
    public class IntegrationTest1
    {
        // Instructions:
        // 1. Add a project reference to the target AppHost project, e.g.:
        //
        //    <ItemGroup>
        //        <ProjectReference Include="../MyAspireApp.AppHost/MyAspireApp.AppHost.csproj" />
        //    </ItemGroup>
        //
        // 2. Uncomment the following example test and update 'Projects.MyAspireApp_AppHost' in GlobalSetup.cs to match your AppHost project:
        //
        //[ClassDataSource<HttpClientDataClass>]
        //[Test]
        //public async Task GetWebResourceRootReturnsOkStatusCode(HttpClientDataClass httpClientData)
        //{
        //    // Arrange
        //    var httpClient = httpClientData.HttpClient;
        //    // Act
        //    var response = await httpClient.GetAsync("/");
        //    // Assert
        //    await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        //}
    }
}
