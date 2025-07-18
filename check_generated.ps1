# Clean and rebuild to ensure fresh generated code
Write-Host "Cleaning previous build..." -ForegroundColor Cyan
Remove-Item -Path "TUnit.TestProject/obj" -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item -Path "TUnit.TestProject/bin" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "`nBuilding project..." -ForegroundColor Cyan
dotnet build TUnit.TestProject -f net8.0 -c Debug

Write-Host "`nSearching for generated SimpleGenericClassTests files..." -ForegroundColor Cyan
Get-ChildItem -Path "TUnit.TestProject" -Filter "*.g.cs" -Recurse | Where-Object { $_.FullName -match "SimpleGenericClassTests" } | ForEach-Object {
    Write-Host "`nFound: $($_.FullName)" -ForegroundColor Green
    Write-Host "Content:" -ForegroundColor Yellow
    Get-Content $_.FullName | Select-Object -First 200
}