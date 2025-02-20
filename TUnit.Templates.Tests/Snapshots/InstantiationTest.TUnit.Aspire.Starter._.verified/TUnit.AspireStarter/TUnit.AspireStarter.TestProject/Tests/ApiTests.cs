using System.Text.Json;
using TUnit.AspireStarter.TestProject.Models;

namespace TUnit.AspireStarter.TestProject.Tests;

public class ApiTests
{
    [Test]
    public async Task GetWeatherForecastReturnsOkStatusCode()
    {
        // Act
        var httpClient = (GlobalHooks.App ?? throw new NullReferenceException()).CreateHttpClient("apiservice");
        if (GlobalHooks.NotificationService != null)
        {
            await GlobalHooks.NotificationService.WaitForResourceAsync("apiservice", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        }
        var response = await httpClient.GetAsync("/weatherforecast");
        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    } 

    [Test]
    [MatrixDataSource]
    public async Task GetWeatherForecastReturnsCorrectData(
        [Matrix("Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching")] string summary
    )
    {
        // Act
        var httpClient = (GlobalHooks.App ?? throw new NullReferenceException()).CreateHttpClient("apiservice");
        if (GlobalHooks.NotificationService != null)
        {
            await GlobalHooks.NotificationService.WaitForResourceAsync("apiservice", KnownResourceStates.Running).WaitAsync(TimeSpan.FromSeconds(30));
        }

        var response = await httpClient.GetAsync("/weatherforecast");
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        // Assert
        await Assert.That(data!.FirstOrDefault(w => w.Summary == summary)).IsNotNull();
    }
}