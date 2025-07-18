Write-Host "Testing generic class fix..." -ForegroundColor Cyan

# Build the test project for .NET 8.0 only
Write-Host "`nBuilding test project for .NET 8.0..." -ForegroundColor Yellow
dotnet build TUnit.TestProject -f net8.0 -c Debug

if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed!" -ForegroundColor Red
    exit 1
}

# Run the specific generic test
Write-Host "`nRunning SimpleGenericClassTests..." -ForegroundColor Yellow
dotnet test TUnit.TestProject --filter "FullyQualifiedName~SimpleGenericClassTests" --framework net8.0 --no-build -v n

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nTests passed!" -ForegroundColor Green
} else {
    Write-Host "`nTests failed!" -ForegroundColor Red
}