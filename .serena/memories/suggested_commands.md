# Suggested Commands for TUnit Development

## Build Commands
```bash
# Debug build
dotnet build

# Release build
dotnet build -c Release

# Clean build artifacts
./clean.ps1

# Build NuGet packages
dotnet pack -c Release
```

## Test Execution Commands
```bash
# Run tests in a project (3 equivalent ways)
cd [TestProjectDirectory]
dotnet run -c Release
dotnet test -c Release
dotnet exec bin/Release/net8.0/TestProject.dll

# Test options
dotnet run -- --list-tests                    # List all tests
dotnet run -- --fail-fast                     # Stop on first failure
dotnet run -- --maximum-parallel-tests 10     # Control parallelism
dotnet run -- --report-trx --coverage         # Generate reports
dotnet run -- --treenode-filter "/*/*/*/TestName"    # Run specific test by exact name
dotnet run -- --treenode-filter/*/*/*PartialName*/*  # Filter tests by partial name pattern
```

## Development Commands
```bash
# Run full pipeline
dotnet run --project TUnit.Pipeline/TUnit.Pipeline.csproj

# Documentation site
cd docs
npm install      # First time only
npm start        # Run locally at localhost:3000
npm run build    # Build static site
```

## System Commands (Linux)
- `ls` - List directory contents
- `find` - Search for files/directories
- `grep` - Search text in files
- `git` - Version control operations
- `cd` - Change directory
