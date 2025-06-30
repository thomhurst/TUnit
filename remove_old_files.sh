#!/bin/bash

# Script to remove old architecture files
# Run with: bash remove_old_files.sh

echo "Removing redundant files from TUnit architecture..."
echo "================================================="

# Discovery Pipeline files
echo "Removing discovery pipeline files..."
rm -f TUnit.Engine/Services/TUnitTestDiscoverer.cs
rm -f TUnit.Engine/Services/BaseTestsConstructor.cs
rm -f TUnit.Engine/Services/TestsCollector.cs
rm -f TUnit.Engine/Services/SourceGeneratedTestsConstructor.cs
rm -f TUnit.Engine/Services/ReflectionTestsConstructor.cs

# Builder files
echo "Removing builder files..."
rm -f TUnit.Core/StaticTestBuilder.cs
rm -f TUnit.Core/DynamicTestBuilder.cs
rm -f TUnit.Engine/Services/ReflectionTestConstructionBuilder.cs
rm -f TUnit.Engine/Services/TestVariationBuilder.cs
rm -f TUnit.Core.SourceGenerator/Builders/StaticTestDefinitionBuilder.cs
rm -f TUnit.Core.SourceGenerator/Builders/DynamicTestMetadataBuilder.cs

# Old test definition types
echo "Removing old test definition types..."
rm -f TUnit.Core.SourceGenerator/Models/TestDefinition.cs
rm -f TUnit.Core.SourceGenerator/Models/StaticTestDefinition.cs
rm -f TUnit.Core.SourceGenerator/Models/DynamicTestMetadata.cs

# Registrars
echo "Removing redundant registrars..."
rm -f TUnit.Engine/Services/DynamicTestRegistrar.cs
rm -f TUnit.Engine/Services/TestRegistrar.cs

# Expanders
echo "Removing expander files..."
rm -f TUnit.Engine/Services/TestMetadataExpander.cs
rm -f TUnit.Engine/Services/TestVariationExpander.cs

echo "================================================="
echo "Removal complete!"
echo ""
echo "Next steps:"
echo "1. Update project files to remove references to deleted files"
echo "2. Update using statements in remaining files"
echo "3. Run tests to ensure nothing is broken"