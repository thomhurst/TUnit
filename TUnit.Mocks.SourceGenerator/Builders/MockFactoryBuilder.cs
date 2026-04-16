using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Builders;

internal static class MockFactoryBuilder
{
    public static void BuildInto(CodeWriter writer, MockTypeModel model)
    {
        var safeName = MockImplBuilder.GetCompositeShortSafeName(model);

        if (model.IsWrapMock)
        {
            BuildWrapFactory(writer, model, safeName);
        }
        else if (model.IsPartialMock)
        {
            BuildPartialFactory(writer, model, safeName);
        }
        else
        {
            BuildInterfaceFactory(writer, model, safeName);
        }
    }

    private static void BuildInterfaceFactory(CodeWriter writer, MockTypeModel model, string safeName)
    {
        var mockableType = MockImplBuilder.GetMockableTypeName(model);
        var factoryClassName = $"{safeName}MockFactory";
        var implTypeName = MockImplBuilder.GetGeneratedTypeName($"{safeName}MockImpl", model);
        var wrapperTypeName = MockImplBuilder.GetGeneratedTypeName($"{safeName}Mock", model);

        using (writer.Block($"internal static class {factoryClassName}"))
        {
            writer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            using (writer.Block("internal static void Register()"))
            {
                if (model.AdditionalInterfaceNames.Length > 0)
                {
                    // Register as multi-interface factory with compound key
                    var allTypes = new[] { model.FullyQualifiedName }
                        .Concat(model.AdditionalInterfaceNames)
                        .Select(t => $"typeof({t}).FullName");
                    var keyExpr = "string.Join(\"|\", new[] { " + string.Join(", ", allTypes) + " })";
                    writer.AppendLine($"global::TUnit.Mocks.MockRegistry.RegisterMultiFactory({keyExpr}, Create);");
                }
                else if (model.TypeParameters.Length > 0)
                {
                    var wrapperTypeExpr = MockWrapperTypeBuilder.CanGenerateWrapper(model)
                        ? GetOpenGeneratedTypeOfExpression($"{safeName}Mock", model)
                        : "null";

                    writer.AppendLine($"global::TUnit.Mocks.MockRegistry.RegisterOpenGenericFactory(");
                    writer.AppendLine($"    typeof({MockImplBuilder.GetOpenGenericTypeOfExpression(model)}),");
                    writer.AppendLine($"    {GetOpenGeneratedTypeOfExpression($"{safeName}MockImpl", model)},");
                    writer.AppendLine($"    {wrapperTypeExpr});");
                }
                else
                {
                    writer.AppendLine($"global::TUnit.Mocks.MockRegistry.RegisterFactory<{mockableType}>(Create);");
                }
            }
            writer.AppendLine();

            {
                var typeParams = MockImplBuilder.GetTypeParameterList(model);
                var constraints = MockImplBuilder.GetConstraintClauses(model);
                using (writer.Block($"internal static global::TUnit.Mocks.Mock<{mockableType}> CreateAutoMock{typeParams}(global::TUnit.Mocks.MockBehavior behavior){constraints}"))
                {
                    EmitCreateInterfaceMockBody(writer, model, mockableType, implTypeName, wrapperTypeName);
                }
                writer.AppendLine();
            }

            if (model.TypeParameters.Length == 0 || model.AdditionalInterfaceNames.Length > 0)
            {
                using (writer.Block($"internal static global::TUnit.Mocks.Mock<{mockableType}> Create(global::TUnit.Mocks.MockBehavior behavior, object[] constructorArgs)"))
                {
                    writer.AppendLine($"if (constructorArgs.Length > 0) throw new global::System.ArgumentException($\"Interface mock '{mockableType}' does not support constructor arguments, but {{constructorArgs.Length}} were provided.\");");
                    EmitCreateInterfaceMockBody(writer, model, mockableType, implTypeName, wrapperTypeName);
                }
            }
        }
    }

    private static string GetOpenGeneratedTypeOfExpression(string baseName, MockTypeModel model)
    {
        if (model.TypeParameters.Length == 0)
            return $"typeof({baseName})";

        return $"typeof({baseName}<{new string(',', model.TypeParameters.Length - 1)}>)";
    }

    private static void EmitCreateInterfaceMockBody(CodeWriter writer, MockTypeModel model, string mockableType, string implTypeName, string wrapperTypeName)
    {
        writer.AppendLine($"var engine = new global::TUnit.Mocks.MockEngine<{mockableType}>(behavior);");
        writer.AppendLine($"var impl = new {implTypeName}(engine);");
        writer.AppendLine("engine.Raisable = impl;");
        if (MockWrapperTypeBuilder.CanGenerateWrapper(model))
        {
            writer.AppendLine($"var mock = new {wrapperTypeName}(impl, engine);");
        }
        else
        {
            writer.AppendLine($"var mock = new global::TUnit.Mocks.Mock<{mockableType}>(impl, engine);");
        }
        writer.AppendLine("return mock;");
    }

    private static void BuildWrapFactory(CodeWriter writer, MockTypeModel model, string safeName)
    {
        using (writer.Block($"file static class {safeName}WrapMockFactory"))
        {
            writer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            using (writer.Block("internal static void Register()"))
            {
                writer.AppendLine($"global::TUnit.Mocks.MockRegistry.RegisterWrapFactory<{model.FullyQualifiedName}>(Create);");
            }
            writer.AppendLine();

            using (writer.Block($"private static global::TUnit.Mocks.Mock<{model.FullyQualifiedName}> Create(global::TUnit.Mocks.MockBehavior behavior, {model.FullyQualifiedName} instance)"))
            {
                writer.AppendLine($"var engine = new global::TUnit.Mocks.MockEngine<{model.FullyQualifiedName}>(behavior);");
                writer.AppendLine("engine.IsWrapMock = true;");
                writer.AppendLine($"var impl = new {safeName}WrapMockImpl(engine, instance);");
                writer.AppendLine("engine.Raisable = impl;");
                writer.AppendLine($"var mock = new global::TUnit.Mocks.Mock<{model.FullyQualifiedName}>(impl, engine);");
                writer.AppendLine("return mock;");
            }
        }
    }

    private static void BuildPartialFactory(CodeWriter writer, MockTypeModel model, string safeName)
    {
        using (writer.Block($"file static class {safeName}PartialMockFactory"))
        {
            writer.AppendLine("[global::System.Runtime.CompilerServices.ModuleInitializer]");
            using (writer.Block("internal static void Register()"))
            {
                writer.AppendLine($"global::TUnit.Mocks.MockRegistry.RegisterFactory<{model.FullyQualifiedName}>(Create);");
            }
            writer.AppendLine();

            using (writer.Block($"private static global::TUnit.Mocks.Mock<{model.FullyQualifiedName}> Create(global::TUnit.Mocks.MockBehavior behavior, object[] constructorArgs)"))
            {
                writer.AppendLine($"var engine = new global::TUnit.Mocks.MockEngine<{model.FullyQualifiedName}>(behavior);");

                GenerateConstructorDispatch(writer, model, safeName);

                writer.AppendLine("engine.Raisable = impl;");
                writer.AppendLine($"var mock = new global::TUnit.Mocks.Mock<{model.FullyQualifiedName}>(impl, engine);");
                writer.AppendLine("return mock;");
            }
        }
    }

    private static void GenerateConstructorDispatch(CodeWriter writer, MockTypeModel model, string safeName)
    {
        if (model.Constructors.Length == 0)
        {
            // No explicit constructors - just use parameterless
            writer.AppendLine($"var impl = new {safeName}MockImpl(engine);");
            return;
        }

        // Check if there's a parameterless constructor
        var hasParameterless = model.Constructors.Any(c => c.Parameters.Length == 0);

        // Sort constructors by parameter count for dispatch
        var orderedCtors = model.Constructors
            .OrderBy(c => c.Parameters.Length)
            .ToList();

        // If there's only a parameterless constructor, simple case
        if (orderedCtors.Count == 1 && hasParameterless)
        {
            writer.AppendLine($"var impl = new {safeName}MockImpl(engine);");
            return;
        }

        // Generate dispatch logic based on constructorArgs length and types
        writer.AppendLine($"{safeName}MockImpl impl;");

        // Group constructors by arity to handle same-arity overloads
        var arityGroups = orderedCtors.GroupBy(c => c.Parameters.Length).OrderBy(g => g.Key).ToList();

        bool first = true;
        foreach (var group in arityGroups)
        {
            var keyword = first ? "if" : "else if";
            first = false;

            var ctorsInGroup = group.ToList();

            if (group.Key == 0)
            {
                using (writer.Block($"{keyword} (constructorArgs.Length == 0)"))
                {
                    writer.AppendLine($"impl = new {safeName}MockImpl(engine);");
                }
            }
            else
            {
                // Type-check dispatch — handles both single and multiple constructors at this arity
                using (writer.Block($"{keyword} (constructorArgs.Length == {group.Key})"))
                {
                    bool innerFirst = true;
                    foreach (var ctor in ctorsInGroup)
                    {
                        var innerKeyword = innerFirst ? "if" : "else if";
                        innerFirst = false;

                        // Build type-check conditions for each parameter
                        // Reference types accept null via (arg is null or Type); value types use (arg is Type)
                        var typeChecks = new List<string>();
                        var castArgs = new List<string>();
                        for (int i = 0; i < ctor.Parameters.Length; i++)
                        {
                            var p = ctor.Parameters[i];
                            typeChecks.Add(p.IsValueType
                                ? $"constructorArgs[{i}] is {p.FullyQualifiedType}"
                                : $"(constructorArgs[{i}] is null or {p.FullyQualifiedType})");
                            castArgs.Add($"({p.FullyQualifiedType})constructorArgs[{i}]");
                        }
                        var condition = string.Join(" && ", typeChecks);
                        var argList = string.Join(", ", castArgs);

                        using (writer.Block($"{innerKeyword} ({condition})"))
                        {
                            writer.AppendLine($"impl = new {safeName}MockImpl(engine, {argList});");
                        }
                    }

                    using (writer.Block("else"))
                    {
                        writer.AppendLine($"throw new global::System.ArgumentException($\"No matching constructor found for type '{model.FullyQualifiedName}' with the provided argument types.\");");
                    }
                }
            }
        }

        using (writer.Block("else"))
        {
            writer.AppendLine($"throw new global::System.ArgumentException($\"No matching constructor found for type '{model.FullyQualifiedName}' with {{constructorArgs.Length}} argument(s).\");");
        }
    }
}
