#!/usr/bin/env pwsh

Write-Host "Testing single file mode fixes for data source discovery" -ForegroundColor Cyan

# Build the test project
Write-Host "`nBuilding TUnit.TestProject..." -ForegroundColor Yellow
dotnet build TUnit.TestProject -c Release

# Run the specific test in different modes
Write-Host "`nRunning ClassDataSourceWithMethodDataSourceTests in normal mode..." -ForegroundColor Yellow
dotnet run --project TUnit.TestProject -c Release --no-build -- --treenode-filter "/*/*/ClassDataSourceWithMethodDataSourceTests/*"

Write-Host "`nRunning ClassDataSourceWithMethodDataSourceTests in reflection mode..." -ForegroundColor Yellow
dotnet run --project TUnit.TestProject -c Release --no-build -- --treenode-filter "/*/*/ClassDataSourceWithMethodDataSourceTests/*" --reflection

Write-Host "`nRunning DiagnosticClassDataSourceTests in normal mode..." -ForegroundColor Yellow
dotnet run --project TUnit.TestProject -c Release --no-build -- --treenode-filter "/*/*/DiagnosticClassDataSourceTests/*"

Write-Host "`nRunning DiagnosticClassDataSourceTests in reflection mode..." -ForegroundColor Yellow
dotnet run --project TUnit.TestProject -c Release --no-build -- --treenode-filter "/*/*/DiagnosticClassDataSourceTests/*" --reflection

Write-Host "`nTo test single file mode, publish as single file and run:" -ForegroundColor Green
Write-Host "dotnet publish TUnit.TestProject -c Release -r win-x64 --self-contained -p:PublishSingleFile=true" -ForegroundColor Gray
Write-Host "Then run the executable with: --treenode-filter `"/*/*/ClassDataSourceWithMethodDataSourceTests/*`"" -ForegroundColor Gray