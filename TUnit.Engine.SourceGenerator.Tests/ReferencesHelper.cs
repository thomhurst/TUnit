﻿using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;

namespace TUnit.Engine.SourceGenerator.Tests;

internal class ReferencesHelper
{
    public static readonly List<PortableExecutableReference> References =
        AppDomain.CurrentDomain.GetAssemblies()
            .Where(x => !x.IsDynamic && !string.IsNullOrWhiteSpace(x.Location))
            .Select(x => MetadataReference.CreateFromFile(x.Location))
            .Concat([
                // add your app/lib specifics, e.g.:
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Assertions.Assert).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Core.TestAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(ServiceProvider).Assembly.Location),
            ])
            .ToList();
}