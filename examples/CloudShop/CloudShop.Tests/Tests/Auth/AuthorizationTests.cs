using System.Net;
using CloudShop.Tests.DataSources;
using CloudShop.Tests.Infrastructure;
using TUnit.Core;
using TUnit.Assertions;
using TUnit.Assertions.Extensions;

namespace CloudShop.Tests.Tests.Auth;

/// <summary>
/// Tests role-based access control across API endpoints.
///
/// Showcases:
/// - [CombinedDataSources] creating Cartesian product of different data source types
/// - Mixing [ClassDataSource] (authenticated clients) with [MethodDataSource] (endpoint scenarios)
/// - Tests admin vs customer access for 5 endpoints × 2 roles = 10 test cases
/// </summary>
[Category("Integration"), Category("Authorization")]
public class AuthorizationTests
{
    [Test]
    [CombinedDataSources]
    public async Task Admin_Has_Full_Access(
        [ClassDataSource<AdminApiClient>(Shared = SharedType.PerTestSession)]
        AdminApiClient admin,
        [MethodDataSource(typeof(OrderDataSources), nameof(OrderDataSources.ProtectedEndpoints))]
        EndpointScenario endpoint)
    {
        var request = new HttpRequestMessage(endpoint.Method, endpoint.Path);
        // Add a body for POST/PUT requests to avoid 400 errors
        if (endpoint.Method == HttpMethod.Post || endpoint.Method == HttpMethod.Put)
        {
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        }

        var response = await admin.Client.SendAsync(request);

        // Admin should never get Forbidden
        await Assert.That(response.StatusCode).IsNotEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    [CombinedDataSources]
    public async Task Customer_Has_Limited_Access(
        [ClassDataSource<CustomerApiClient>(Shared = SharedType.PerTestSession)]
        CustomerApiClient customer,
        [MethodDataSource(typeof(OrderDataSources), nameof(OrderDataSources.ProtectedEndpoints))]
        EndpointScenario endpoint)
    {
        var request = new HttpRequestMessage(endpoint.Method, endpoint.Path);
        if (endpoint.Method == HttpMethod.Post || endpoint.Method == HttpMethod.Put)
        {
            request.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
        }

        var response = await customer.Client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(endpoint.ExpectedForCustomer);
    }

    [Test]
    [CombinedDataSources]
    public async Task Unauthenticated_Requests_Are_Rejected(
        [ClassDataSource<ApiClientFixture>(Shared = SharedType.PerTestSession)]
        ApiClientFixture unauthenticated)
    {
        var response = await unauthenticated.Client.GetAsync("/api/orders/mine");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}
