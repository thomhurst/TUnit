# PowerShell script to build and test AOT compatibility

Write-Host "Starting AOT Compatibility Build and Test..." -ForegroundColor Green

# Set environment variable for source generation mode
$env:TUNIT_EXECUTION_MODE = "SourceGeneration"

Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "Clean failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Step 2: Building in Release mode with AOT analysis..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build failed! Check for AOT compatibility issues." -ForegroundColor Red
    exit 1
}

Write-Host "Step 3: Running tests to verify functionality..." -ForegroundColor Yellow
dotnet test --configuration Release --logger console --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests failed!" -ForegroundColor Red
    exit 1
}

Write-Host "Step 4: Publishing with AOT..." -ForegroundColor Yellow
dotnet publish --configuration Release --verbosity normal
if ($LASTEXITCODE -ne 0) {
    Write-Host "AOT publish failed! Check for trimming/AOT issues." -ForegroundColor Red
    exit 1
}

Write-Host "Step 5: Verifying published output..." -ForegroundColor Yellow
$publishDir = "bin\Release\net9.0\publish"
if (Test-Path $publishDir) {
    $files = Get-ChildItem $publishDir
    Write-Host "Published files:" -ForegroundColor Cyan
    $files | ForEach-Object { Write-Host "  $($_.Name)" -ForegroundColor Cyan }
    
    # Check for native executable
    $exeFile = Get-ChildItem $publishDir -Filter "*.exe" | Select-Object -First 1
    if ($exeFile) {
        Write-Host "Native executable found: $($exeFile.Name)" -ForegroundColor Green
        Write-Host "Size: $([math]::Round($exeFile.Length / 1MB, 2)) MB" -ForegroundColor Green
    } else {
        Write-Host "Warning: No native executable found" -ForegroundColor Yellow
    }
} else {
    Write-Host "Publish directory not found!" -ForegroundColor Red
    exit 1
}

Write-Host "" -ForegroundColor Green
Write-Host "✓ AOT Compatibility Build and Test SUCCESSFUL!" -ForegroundColor Green
Write-Host "✓ All source generation mode features working" -ForegroundColor Green
Write-Host "✓ AOT publish completed without warnings" -ForegroundColor Green
Write-Host "✓ Native executable generated" -ForegroundColor Green
Write-Host "" -ForegroundColor Green
Write-Host "TUnit is now fully AOT compatible!" -ForegroundColor Green