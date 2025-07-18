Write-Host "Testing generic test combinations..."
Set-Location TUnit.TestProject
dotnet test --filter "FullyQualifiedName~SimpleGenericClassTests" --logger "console;verbosity=minimal" 2>&1 | Select-String -Pattern "Passed|Failed|Total|Error|test" | ForEach-Object { Write-Host $_ }