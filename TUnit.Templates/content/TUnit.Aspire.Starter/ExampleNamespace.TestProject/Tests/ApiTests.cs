using System.Text.Json;
using ExampleNamespace.TestProject.Data;
using ExampleNamespace.TestProject.Models;

namespace ExampleNamespace.TestProject.Tests;

[ClassDataSource<HttpClientDataClass>]
public class ApiTests(HttpClientDataClass httpClientData)
{
    [Test]
    public async Task GetWeatherForecastReturnsOkStatusCode()
    {
        // Arrange
        var httpClient = httpClientData.HttpClient;
        // Act
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
        // Arrange
        var httpClient = httpClientData.HttpClient;
        // Act
        var response = await httpClient.GetAsync("/weatherforecast");
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<IEnumerable<WeatherForecast>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        // Assert
        await Assert.That(data!.FirstOrDefault(w => w.Summary == summary)).IsNotNull();
    }
}