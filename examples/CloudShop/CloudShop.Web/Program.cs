using System.Net;
using CloudShop.Shared.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddHttpClient("api", client => client.BaseAddress = new Uri("http://apiservice"));

var app = builder.Build();

app.UseExceptionHandler("/error");

app.MapGet("/", () => Results.Content("""
    <!DOCTYPE html>
    <html><head><title>CloudShop</title></head>
    <body>
        <h1>CloudShop</h1>
        <p>Welcome to CloudShop! This is a demo application for testing with TUnit + Aspire.</p>
        <ul>
            <li><a href="/products">Browse Products</a></li>
        </ul>
    </body></html>
    """, "text/html"));

app.MapGet("/products", async (IHttpClientFactory httpClientFactory) =>
{
    var client = httpClientFactory.CreateClient("api");
    var products = await client.GetFromJsonAsync<PagedResult<ProductResponse>>("/api/products");

    var html = "<html><head><title>Products - CloudShop</title></head><body>";
    html += "<h1>Products</h1><ul>";
    if (products?.Items is not null)
    {
        foreach (var p in products.Items)
        {
            html += $"<li><strong>{WebUtility.HtmlEncode(p.Name)}</strong> ({WebUtility.HtmlEncode(p.Category)}) - ${p.Price:F2}</li>";
        }
    }
    html += "</ul><a href=\"/\">Home</a></body></html>";

    return Results.Content(html, "text/html");
});

app.MapGet("/error", () => Results.Problem("An error occurred"));

app.MapDefaultEndpoints();
app.Run();
