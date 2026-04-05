using System.Linq;
using TUnit.Mocks.SourceGenerator.Models;

namespace TUnit.Mocks.SourceGenerator.Builders;

internal static class MockImplBuilder
{
    public static void BuildInto(CodeWriter writer, MockTypeModel model)
    {
        var safeName = GetCompositeShortSafeName(model);

        if (model.IsWrapMock)
        {
            BuildWrapMockImpl(writer, model, safeName);
        }
        else if (model.IsPartialMock)
        {
            BuildPartialMockImpl(writer, model, safeName);
        }
        else
        {
            BuildInterfaceMockImpl(writer, model, safeName);
        }
    }

    private static void BuildInterfaceMockImpl(CodeWriter writer, MockTypeModel model, string safeName)
    {
        var mockableType = GetMockableTypeName(model);

        var baseTypes = model.HasStaticAbstractMembers
            ? mockableType  // inherit bridge interface (which : original interface)
            : model.FullyQualifiedName;
        if (model.AdditionalInterfaceNames.Length > 0)
        {
            baseTypes += ", " + string.Join(", ", model.AdditionalInterfaceNames);
        }

        using (writer.Block($"file sealed class {safeName}MockImpl : {baseTypes}, global::TUnit.Mocks.IRaisable, global::TUnit.Mocks.IMockObject"))
        {
            writer.AppendLine($"private readonly global::TUnit.Mocks.MockEngine<{mockableType}> _engine;");
            writer.AppendLine();

            EmitIMockObjectProperty(writer);

            // Constructor
            using (writer.Block($"internal {safeName}MockImpl(global::TUnit.Mocks.MockEngine<{mockableType}> engine)"))
            {
                writer.AppendLine("_engine = engine;");
                if (model.HasStaticAbstractMembers)
                {
                    EmitStaticEngineAssignment(writer, safeName);
                }
            }

            // Methods — skip static abstract (they're in bridge DIMs)
            foreach (var method in model.Methods)
            {
                if (method.IsStaticAbstract) continue;
                writer.AppendLine();
                GenerateInterfaceMethod(writer, method, model);
            }

            // Properties — skip static abstract (they're in bridge DIMs)
            foreach (var prop in model.Properties)
            {
                if (prop.IsIndexer) continue;
                if (prop.IsStaticAbstract) continue;
                writer.AppendLine();
                GenerateInterfaceProperty(writer, prop, model);
            }

            // Events — skip static abstract (they're in bridge DIMs)
            foreach (var evt in model.Events)
            {
                if (evt.IsStaticAbstract) continue;
                writer.AppendLine();
                GenerateEvent(writer, evt);
            }

            // IRaisable.RaiseEvent dispatch
            writer.AppendLine();
            GenerateRaiseEventDispatch(writer, model);
        }
    }

    private static void BuildWrapMockImpl(CodeWriter writer, MockTypeModel model, string safeName)
    {
        var mockableType = GetMockableTypeName(model);

        using (writer.Block($"file sealed class {safeName}WrapMockImpl : {model.FullyQualifiedName}, global::TUnit.Mocks.IRaisable, global::TUnit.Mocks.IMockObject"))
        {
            writer.AppendLine($"private readonly global::TUnit.Mocks.MockEngine<{mockableType}> _engine;");
            writer.AppendLine($"private readonly {model.FullyQualifiedName} _wrappedInstance;");
            writer.AppendLine();

            EmitIMockObjectProperty(writer);

            // Generate constructors that pass through to base + accept wrapped instance
            GenerateWrapConstructors(writer, model, safeName);

            // Methods — skip static abstract (they're in bridge DIMs)
            foreach (var method in model.Methods)
            {
                if (method.IsStaticAbstract) continue;
                writer.AppendLine();
                GenerateWrapMethod(writer, method, model);
            }

            // Properties — skip static abstract (they're in bridge DIMs)
            foreach (var prop in model.Properties)
            {
                if (prop.IsIndexer) continue;
                if (prop.IsStaticAbstract) continue;
                writer.AppendLine();
                GenerateWrapProperty(writer, prop, model);
            }

            // Events — skip static abstract (they're in bridge DIMs)
            foreach (var evt in model.Events)
            {
                if (evt.IsStaticAbstract) continue;
                writer.AppendLine();
                GeneratePartialEvent(writer, evt);
            }

            // IRaisable.RaiseEvent dispatch
            writer.AppendLine();
            GenerateRaiseEventDispatch(writer, model);
        }
    }

    private static void GenerateWrapConstructors(CodeWriter writer, MockTypeModel model, string safeName)
    {
        var mockableType = GetMockableTypeName(model);

        if (model.Constructors.Length == 0)
        {
            using (writer.Block($"internal {safeName}WrapMockImpl(global::TUnit.Mocks.MockEngine<{mockableType}> engine, {model.FullyQualifiedName} wrappedInstance)"))
            {
                writer.AppendLine("_engine = engine;");
                writer.AppendLine("_wrappedInstance = wrappedInstance;");
                if (model.HasStaticAbstractMembers)
                {
                    EmitStaticEngineAssignment(writer, safeName);
                }
            }
            return;
        }

        foreach (var ctor in model.Constructors)
        {
            if (ctor.Parameters.Length == 0)
            {
                using (writer.Block($"internal {safeName}WrapMockImpl(global::TUnit.Mocks.MockEngine<{mockableType}> engine, {model.FullyQualifiedName} wrappedInstance) : base()"))
                {
                    writer.AppendLine("_engine = engine;");
                    writer.AppendLine("_wrappedInstance = wrappedInstance;");
                    if (model.HasStaticAbstractMembers)
                    {
                        EmitStaticEngineAssignment(writer, safeName);
                    }
                }
            }
            else
            {
                var paramList = string.Join(", ", ctor.Parameters.Select(p => $"{p.FullyQualifiedType} {p.Name}"));
                var argList = string.Join(", ", ctor.Parameters.Select(p => p.Name));
                using (writer.Block($"internal {safeName}WrapMockImpl(global::TUnit.Mocks.MockEngine<{mockableType}> engine, {model.FullyQualifiedName} wrappedInstance, {paramList}) : base({argList})"))
                {
                    writer.AppendLine("_engine = engine;");
                    writer.AppendLine("_wrappedInstance = wrappedInstance;");
                    if (model.HasStaticAbstractMembers)
                    {
                        EmitStaticEngineAssignment(writer, safeName);
                    }
                }
            }
        }
    }

    private static void GenerateWrapMethod(CodeWriter writer, MockMemberModel method, MockTypeModel model)
    {
        var signatureReturnType = (method.IsVoid && !method.IsAsync) ? "void" : method.ReturnType;
        var paramList = GetParameterList(method);
        var typeParams = GetTypeParameterList(method);

        // C# prohibits restating generic constraints on override methods (CS0460)
        var accessModifier = method.IsProtected ? "protected" : "public";
        using (writer.Block($"{accessModifier} override {signatureReturnType} {method.Name}{typeParams}({paramList})"))
        {
            if (method.IsAbstractMember)
            {
                // Abstract methods: dispatch through engine (wrapped instance can't have abstract methods by definition,
                // but we still handle it for consistency)
                GenerateEngineDispatchBody(writer, method);
            }
            else
            {
                // Virtual/override methods: try engine first, fall back to wrapped instance
                GenerateWrapMethodBody(writer, method);
            }
        }
    }

    private static void GenerateWrapMethodBody(CodeWriter writer, MockMemberModel method)
    {
        // Initialize out parameters
        foreach (var p in method.Parameters)
        {
            if (p.Direction == ParameterDirection.Out)
            {
                writer.AppendLine($"{p.Name} = default!;");
            }
        }

        var (isTyped, typeArgs, argsList) = GetTypedDispatchInfo(method);
        var argsArray = isTyped ? null : EmitArgsArrayVariable(writer, method);

        var argPassList = GetArgPassList(method);

        if (method.IsVoid && !method.IsAsync)
        {
            writer.AppendLine($"if ({EmitTryHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            EmitOutRefReadback(writer, method);
            writer.AppendLine("return;");
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"_wrappedInstance.{method.Name}({argPassList});");
        }
        else if (method.IsVoid && method.IsAsync)
        {
            writer.AppendLine($"if ({EmitTryHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            EmitOutRefReadback(writer, method);
            if (method.IsValueTask)
            {
                writer.AppendLine("return default(global::System.Threading.Tasks.ValueTask);");
            }
            else
            {
                writer.AppendLine("return global::System.Threading.Tasks.Task.CompletedTask;");
            }
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return _wrappedInstance.{method.Name}({argPassList});");
        }
        else if (method.IsAsync)
        {
            if (method.IsReturnTypeStaticAbstractInterface)
            {
                writer.AppendLine($"if ({EmitTryHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, "object?", method.MemberId, method.Name, "null", "__rawResult")})");
                writer.AppendLine("{");
                writer.IncreaseIndent();
                writer.AppendLine($"var __result = ({method.UnwrappedReturnType})__rawResult!;");
            }
            else
            {
                writer.AppendLine($"if ({EmitTryHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, method.UnwrappedReturnType, method.MemberId, method.Name, method.UnwrappedSmartDefault, "__result")})");
                writer.AppendLine("{");
                writer.IncreaseIndent();
            }
            EmitOutRefReadback(writer, method);
            if (method.IsValueTask)
            {
                writer.AppendLine($"return new global::System.Threading.Tasks.ValueTask<{method.UnwrappedReturnType}>(__result);");
            }
            else
            {
                writer.AppendLine($"return global::System.Threading.Tasks.Task.FromResult<{method.UnwrappedReturnType}>(__result);");
            }
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return _wrappedInstance.{method.Name}({argPassList});");
        }
        else if (method.IsRefStructReturn)
        {
            writer.AppendLine($"if ({EmitTryHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            if (method.SpanReturnElementType is not null)
            {
                EmitSpanReturnReadback(writer, method);
            }
            else
            {
                EmitOutRefReadback(writer, method);
                writer.AppendLine("return default;");
            }
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return _wrappedInstance.{method.Name}({argPassList});");
        }
        else if (method.IsReturnTypeStaticAbstractInterface)
        {
            writer.AppendLine($"if ({EmitTryHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, "object?", method.MemberId, method.Name, "null", "__rawResult")})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            writer.AppendLine($"var __result = ({method.ReturnType})__rawResult!;");
            EmitOutRefReadback(writer, method);
            writer.AppendLine("return __result;");
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return _wrappedInstance.{method.Name}({argPassList});");
        }
        else
        {
            writer.AppendLine($"if ({EmitTryHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, method.ReturnType, method.MemberId, method.Name, method.SmartDefault, "__result")})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            EmitOutRefReadback(writer, method);
            writer.AppendLine("return __result;");
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return _wrappedInstance.{method.Name}({argPassList});");
        }
    }

    private static void GenerateWrapProperty(CodeWriter writer, MockMemberModel prop, MockTypeModel model)
    {
        var accessModifier = prop.IsProtected ? "protected" : "public";
        writer.AppendLine($"{accessModifier} override {prop.ReturnType} {prop.Name}");
        writer.OpenBrace();

        if (prop.HasGetter)
        {
            if (prop.IsRefStructReturn)
            {
                if (prop.IsAbstractMember)
                {
                    writer.AppendLine("get");
                    writer.OpenBrace();
                    writer.AppendLine($"_engine.HandleCall({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>());");
                    writer.AppendLine("return default;");
                    writer.CloseBrace();
                }
                else
                {
                    writer.AppendLine("get");
                    writer.OpenBrace();
                    writer.AppendLine($"if (_engine.TryHandleCall({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>()))");
                    writer.AppendLine("{");
                    writer.IncreaseIndent();
                    writer.AppendLine("return default;");
                    writer.DecreaseIndent();
                    writer.AppendLine("}");
                    writer.AppendLine($"return _wrappedInstance.{prop.Name};");
                    writer.CloseBrace();
                }
            }
            else if (prop.IsAbstractMember && prop.IsReturnTypeStaticAbstractInterface)
            {
                writer.AppendLine($"get => ({prop.ReturnType})_engine.HandleCallWithReturn<object?>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), null)!;");
            }
            else if (prop.IsAbstractMember)
            {
                writer.AppendLine($"get => _engine.HandleCallWithReturn<{prop.ReturnType}>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), {prop.SmartDefault});");
            }
            else if (prop.IsReturnTypeStaticAbstractInterface)
            {
                writer.AppendLine("get");
                writer.OpenBrace();
                writer.AppendLine($"if (_engine.TryHandleCallWithReturn<object?>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), null, out var __rawResult))");
                writer.AppendLine("{");
                writer.IncreaseIndent();
                writer.AppendLine($"return ({prop.ReturnType})__rawResult!;");
                writer.DecreaseIndent();
                writer.AppendLine("}");
                writer.AppendLine($"return _wrappedInstance.{prop.Name};");
                writer.CloseBrace();
            }
            else
            {
                writer.AppendLine("get");
                writer.OpenBrace();
                writer.AppendLine($"if (_engine.TryHandleCallWithReturn<{prop.ReturnType}>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), {prop.SmartDefault}, out var __result))");
                writer.AppendLine("{");
                writer.IncreaseIndent();
                writer.AppendLine("return __result;");
                writer.DecreaseIndent();
                writer.AppendLine("}");
                writer.AppendLine($"return _wrappedInstance.{prop.Name};");
                writer.CloseBrace();
            }
        }

        if (prop.HasSetter)
        {
            var setterArgs = prop.IsRefStructReturn
                ? "global::System.Array.Empty<object?>()"
                : "new object?[] { value }";

            if (prop.IsAbstractMember)
            {
                writer.AppendLine($"set => _engine.HandleCall({prop.SetterMemberId}, \"set_{prop.Name}\", {setterArgs});");
            }
            else
            {
                writer.AppendLine("set");
                writer.OpenBrace();
                writer.AppendLine($"if (!_engine.TryHandleCall({prop.SetterMemberId}, \"set_{prop.Name}\", {setterArgs}))");
                writer.AppendLine("{");
                writer.IncreaseIndent();
                writer.AppendLine($"_wrappedInstance.{prop.Name} = value;");
                writer.DecreaseIndent();
                writer.AppendLine("}");
                writer.CloseBrace();
            }
        }

        writer.CloseBrace();
    }

    private static void BuildPartialMockImpl(CodeWriter writer, MockTypeModel model, string safeName)
    {
        var mockableType = GetMockableTypeName(model);

        using (writer.Block($"file sealed class {safeName}MockImpl : {model.FullyQualifiedName}, global::TUnit.Mocks.IRaisable, global::TUnit.Mocks.IMockObject"))
        {
            writer.AppendLine($"private readonly global::TUnit.Mocks.MockEngine<{mockableType}> _engine;");
            writer.AppendLine();

            EmitIMockObjectProperty(writer);

            // Generate constructors that pass through to base
            GeneratePartialConstructors(writer, model, safeName);

            // Methods — skip static abstract (they're in bridge DIMs)
            foreach (var method in model.Methods)
            {
                if (method.IsStaticAbstract) continue;
                writer.AppendLine();
                GeneratePartialMethod(writer, method, model);
            }

            // Properties — skip static abstract (they're in bridge DIMs)
            foreach (var prop in model.Properties)
            {
                if (prop.IsIndexer) continue;
                if (prop.IsStaticAbstract) continue;
                writer.AppendLine();
                GeneratePartialProperty(writer, prop, model);
            }

            // Events — skip static abstract (they're in bridge DIMs)
            foreach (var evt in model.Events)
            {
                if (evt.IsStaticAbstract) continue;
                writer.AppendLine();
                GeneratePartialEvent(writer, evt);
            }

            // IRaisable.RaiseEvent dispatch
            writer.AppendLine();
            GenerateRaiseEventDispatch(writer, model);
        }
    }

    private static void GeneratePartialConstructors(CodeWriter writer, MockTypeModel model, string safeName)
    {
        var mockableType = GetMockableTypeName(model);

        if (model.Constructors.Length == 0)
        {
            // No explicit constructors found, generate a default one
            using (writer.Block($"internal {safeName}MockImpl(global::TUnit.Mocks.MockEngine<{mockableType}> engine)"))
            {
                writer.AppendLine("_engine = engine;");
                if (model.HasStaticAbstractMembers)
                {
                    EmitStaticEngineAssignment(writer, safeName);
                }
            }
            return;
        }

        foreach (var ctor in model.Constructors)
        {
            if (ctor.Parameters.Length == 0)
            {
                // Parameterless constructor
                using (writer.Block($"internal {safeName}MockImpl(global::TUnit.Mocks.MockEngine<{mockableType}> engine) : base()"))
                {
                    writer.AppendLine("_engine = engine;");
                    if (model.HasStaticAbstractMembers)
                    {
                        EmitStaticEngineAssignment(writer, safeName);
                    }
                }
            }
            else
            {
                // Constructor with parameters - pass them through to base
                var paramList = string.Join(", ", ctor.Parameters.Select(p => $"{p.FullyQualifiedType} {p.Name}"));
                var argList = string.Join(", ", ctor.Parameters.Select(p => p.Name));
                using (writer.Block($"internal {safeName}MockImpl(global::TUnit.Mocks.MockEngine<{mockableType}> engine, {paramList}) : base({argList})"))
                {
                    writer.AppendLine("_engine = engine;");
                    if (model.HasStaticAbstractMembers)
                    {
                        EmitStaticEngineAssignment(writer, safeName);
                    }
                }
            }
        }
    }

    private static void GenerateInterfaceMethod(CodeWriter writer, MockMemberModel method, MockTypeModel model)
    {
        var signatureReturnType = (method.IsVoid && !method.IsAsync) ? "void" : method.ReturnType;
        var paramList = GetParameterList(method);
        var typeParams = GetTypeParameterList(method);
        var constraints = GetConstraintClauses(method);

        using (writer.Block($"public {signatureReturnType} {method.Name}{typeParams}({paramList}){constraints}"))
        {
            GenerateEngineDispatchBody(writer, method);
        }
    }

    private static void GeneratePartialMethod(CodeWriter writer, MockMemberModel method, MockTypeModel model)
    {
        var signatureReturnType = (method.IsVoid && !method.IsAsync) ? "void" : method.ReturnType;
        var paramList = GetParameterList(method);
        var typeParams = GetTypeParameterList(method);

        // C# prohibits restating generic constraints on override methods (CS0460)
        var accessModifier = method.IsProtected ? "protected" : "public";
        using (writer.Block($"{accessModifier} override {signatureReturnType} {method.Name}{typeParams}({paramList})"))
        {
            if (method.IsAbstractMember)
            {
                // Abstract methods: same as interface methods - dispatch through engine
                GenerateEngineDispatchBody(writer, method);
            }
            else
            {
                // Virtual/override methods: try engine first, fall back to base
                GeneratePartialMethodBody(writer, method);
            }
        }
    }

    private static void GeneratePartialMethodBody(CodeWriter writer, MockMemberModel method)
    {
        // Initialize out parameters
        foreach (var p in method.Parameters)
        {
            if (p.Direction == ParameterDirection.Out)
            {
                writer.AppendLine($"{p.Name} = default!;");
            }
        }

        var (isTyped, typeArgs, argsList) = GetTypedDispatchInfo(method);
        var argsArray = isTyped ? null : EmitArgsArrayVariable(writer, method);

        var argPassList = GetArgPassList(method);

        if (method.IsVoid && !method.IsAsync)
        {
            // void virtual method
            writer.AppendLine($"if ({EmitTryHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            EmitOutRefReadback(writer, method);
            writer.AppendLine("return;");
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"base.{method.Name}({argPassList});");
        }
        else if (method.IsVoid && method.IsAsync)
        {
            // async void virtual method (Task/ValueTask)
            writer.AppendLine($"if ({EmitTryHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            EmitOutRefReadback(writer, method);
            EmitRawReturnCheck(writer, method);
            if (method.IsValueTask)
            {
                writer.AppendLine("return default(global::System.Threading.Tasks.ValueTask);");
            }
            else
            {
                writer.AppendLine("return global::System.Threading.Tasks.Task.CompletedTask;");
            }
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return base.{method.Name}({argPassList});");
        }
        else if (method.IsAsync)
        {
            // async method with return (Task<T>/ValueTask<T>)
            if (method.IsReturnTypeStaticAbstractInterface)
            {
                writer.AppendLine($"if ({EmitTryHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, "object?", method.MemberId, method.Name, "null", "__rawResult")})");
                writer.AppendLine("{");
                writer.IncreaseIndent();
                writer.AppendLine($"var __result = ({method.UnwrappedReturnType})__rawResult!;");
            }
            else
            {
                writer.AppendLine($"if ({EmitTryHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, method.UnwrappedReturnType, method.MemberId, method.Name, method.UnwrappedSmartDefault, "__result")})");
                writer.AppendLine("{");
                writer.IncreaseIndent();
            }
            EmitOutRefReadback(writer, method);
            EmitRawReturnCheck(writer, method);
            if (method.IsValueTask)
            {
                writer.AppendLine($"return new global::System.Threading.Tasks.ValueTask<{method.UnwrappedReturnType}>(__result);");
            }
            else
            {
                writer.AppendLine($"return global::System.Threading.Tasks.Task.FromResult<{method.UnwrappedReturnType}>(__result);");
            }
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return base.{method.Name}({argPassList});");
        }
        else if (method.IsRefStructReturn)
        {
            // synchronous method returning ref struct — use void dispatch, fall back to base
            writer.AppendLine($"if ({EmitTryHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            if (method.SpanReturnElementType is not null)
            {
                EmitSpanReturnReadback(writer, method);
            }
            else
            {
                EmitOutRefReadback(writer, method);
                writer.AppendLine("return default;");
            }
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return base.{method.Name}({argPassList});");
        }
        else if (method.IsReturnTypeStaticAbstractInterface)
        {
            writer.AppendLine($"if ({EmitTryHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, "object?", method.MemberId, method.Name, "null", "__rawResult")})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            writer.AppendLine($"var __result = ({method.ReturnType})__rawResult!;");
            EmitOutRefReadback(writer, method);
            writer.AppendLine("return __result;");
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return base.{method.Name}({argPassList});");
        }
        else
        {
            // synchronous method with return value
            writer.AppendLine($"if ({EmitTryHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, method.ReturnType, method.MemberId, method.Name, method.SmartDefault, "__result")})");
            writer.AppendLine("{");
            writer.IncreaseIndent();
            EmitOutRefReadback(writer, method);
            writer.AppendLine("return __result;");
            writer.DecreaseIndent();
            writer.AppendLine("}");
            writer.AppendLine($"return base.{method.Name}({argPassList});");
        }
    }

    private static void GenerateEngineDispatchBody(CodeWriter writer, MockMemberModel method)
    {
        // Initialize out parameters
        foreach (var p in method.Parameters)
        {
            if (p.Direction == ParameterDirection.Out)
            {
                writer.AppendLine($"{p.Name} = default!;");
            }
        }

        var (isTyped, typeArgs, argsList) = GetTypedDispatchInfo(method);
        var argsArray = isTyped ? null : EmitArgsArrayVariable(writer, method);

        var hasOutRef = HasOutRefParams(method);

        if (method.IsVoid && !method.IsAsync)
        {
            // Pure void method
            writer.AppendLine($"{EmitHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)};");
            EmitOutRefReadback(writer, method);
        }
        else if (method.IsVoid && method.IsAsync)
        {
            // Async void method (Task or ValueTask with no generic arg)
            using (writer.Block("try"))
            {
                writer.AppendLine($"{EmitHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)};");
                EmitOutRefReadback(writer, method);
                EmitRawReturnCheck(writer, method);
                if (method.IsValueTask)
                {
                    writer.AppendLine("return default(global::System.Threading.Tasks.ValueTask);");
                }
                else
                {
                    writer.AppendLine("return global::System.Threading.Tasks.Task.CompletedTask;");
                }
            }
            using (writer.Block("catch (global::System.Exception __ex)"))
            {
                if (method.IsValueTask)
                {
                    writer.AppendLine("return new global::System.Threading.Tasks.ValueTask(global::System.Threading.Tasks.Task.FromException(__ex));");
                }
                else
                {
                    writer.AppendLine("return global::System.Threading.Tasks.Task.FromException(__ex);");
                }
            }
        }
        else if (method.IsAsync)
        {
            // Async method with return value (Task<T> or ValueTask<T>)
            var unwrappedArg = method.IsReturnTypeStaticAbstractInterface ? "object?" : method.UnwrappedReturnType;
            var unwrappedDefault = method.IsReturnTypeStaticAbstractInterface ? "null" : method.UnwrappedSmartDefault;
            using (writer.Block("try"))
            {
                if (method.IsReturnTypeStaticAbstractInterface)
                {
                    writer.AppendLine($"var __result = ({method.UnwrappedReturnType}){EmitHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, unwrappedArg, method.MemberId, method.Name, unwrappedDefault)}!;");
                }
                else
                {
                    writer.AppendLine($"var __result = {EmitHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, unwrappedArg, method.MemberId, method.Name, unwrappedDefault)};");
                }
                EmitOutRefReadback(writer, method);
                EmitRawReturnCheck(writer, method);
                if (method.IsValueTask)
                {
                    writer.AppendLine($"return new global::System.Threading.Tasks.ValueTask<{method.UnwrappedReturnType}>(__result);");
                }
                else
                {
                    writer.AppendLine($"return global::System.Threading.Tasks.Task.FromResult<{method.UnwrappedReturnType}>(__result);");
                }
            }
            using (writer.Block("catch (global::System.Exception __ex)"))
            {
                if (method.IsValueTask)
                {
                    writer.AppendLine($"return new global::System.Threading.Tasks.ValueTask<{method.UnwrappedReturnType}>(global::System.Threading.Tasks.Task.FromException<{method.UnwrappedReturnType}>(__ex));");
                }
                else
                {
                    writer.AppendLine($"return global::System.Threading.Tasks.Task.FromException<{method.UnwrappedReturnType}>(__ex);");
                }
            }
        }
        else if (method.IsRefStructReturn)
        {
            // Synchronous method returning a ref struct — can't use HandleCallWithReturn<T> because
            // ref structs can't be generic type arguments. Use void dispatch for call tracking,
            // callbacks, and throws.
            writer.AppendLine($"{EmitHandleCall(isTyped, typeArgs, argsList, argsArray, method.MemberId, method.Name)};");
            if (method.SpanReturnElementType is not null)
            {
                // Span return: read back out/ref params AND extract return value from OutRefContext index -1
                EmitSpanReturnReadback(writer, method);
            }
            else
            {
                EmitOutRefReadback(writer, method);
                writer.AppendLine("return default;");
            }
        }
        else if (method.IsReturnTypeStaticAbstractInterface)
        {
            // Return type is an interface with static abstract members — CS8920 prevents using it
            // as a generic type argument. Use object? and cast.
            if (hasOutRef)
            {
                writer.AppendLine($"var __result = ({method.ReturnType}){EmitHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, "object?", method.MemberId, method.Name, "null")}!;");
                EmitOutRefReadback(writer, method);
                writer.AppendLine("return __result;");
            }
            else
            {
                writer.AppendLine($"return ({method.ReturnType}){EmitHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, "object?", method.MemberId, method.Name, "null")}!;");
            }
        }
        else
        {
            // Synchronous method with return value — need to read back out/ref before returning
            if (hasOutRef)
            {
                writer.AppendLine($"var __result = {EmitHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, method.ReturnType, method.MemberId, method.Name, method.SmartDefault)};");
                EmitOutRefReadback(writer, method);
                writer.AppendLine("return __result;");
            }
            else
            {
                writer.AppendLine($"return {EmitHandleCallWithReturn(isTyped, typeArgs, argsList, argsArray, method.ReturnType, method.MemberId, method.Name, method.SmartDefault)};");
            }
        }
    }

    private static void GenerateInterfaceProperty(CodeWriter writer, MockMemberModel prop, MockTypeModel model)
    {
        writer.AppendLine($"public {prop.ReturnType} {prop.Name}");
        writer.OpenBrace();

        if (prop.HasGetter)
        {
            if (prop.IsRefStructReturn)
            {
                // ref struct property — can't use HandleCallWithReturn<T>, use void dispatch + return default
                writer.AppendLine("get");
                writer.OpenBrace();
                writer.AppendLine($"_engine.HandleCall({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>());");
                writer.AppendLine("return default;");
                writer.CloseBrace();
            }
            else if (prop.IsReturnTypeStaticAbstractInterface)
            {
                writer.AppendLine($"get => ({prop.ReturnType})_engine.HandleCallWithReturn<object?>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), null)!;");
            }
            else
            {
                writer.AppendLine($"get => _engine.HandleCallWithReturn<{prop.ReturnType}>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), {prop.SmartDefault});");
            }
        }

        if (prop.HasSetter)
        {
            if (prop.IsRefStructReturn)
            {
                // ref struct property — can't box value, use empty args
                writer.AppendLine($"set => _engine.HandleCall({prop.SetterMemberId}, \"set_{prop.Name}\", global::System.Array.Empty<object?>());");
            }
            else
            {
                writer.AppendLine($"set => _engine.HandleCall({prop.SetterMemberId}, \"set_{prop.Name}\", new object?[] {{ value }});");
            }
        }

        writer.CloseBrace();
    }

    private static void GeneratePartialProperty(CodeWriter writer, MockMemberModel prop, MockTypeModel model)
    {
        var accessModifier = prop.IsProtected ? "protected" : "public";
        writer.AppendLine($"{accessModifier} override {prop.ReturnType} {prop.Name}");
        writer.OpenBrace();

        if (prop.HasGetter)
        {
            if (prop.IsRefStructReturn)
            {
                if (prop.IsAbstractMember)
                {
                    writer.AppendLine("get");
                    writer.OpenBrace();
                    writer.AppendLine($"_engine.HandleCall({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>());");
                    writer.AppendLine("return default;");
                    writer.CloseBrace();
                }
                else
                {
                    writer.AppendLine("get");
                    writer.OpenBrace();
                    writer.AppendLine($"if (_engine.TryHandleCall({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>()))");
                    writer.AppendLine("{");
                    writer.IncreaseIndent();
                    writer.AppendLine("return default;");
                    writer.DecreaseIndent();
                    writer.AppendLine("}");
                    writer.AppendLine($"return base.{prop.Name};");
                    writer.CloseBrace();
                }
            }
            else if (prop.IsAbstractMember && prop.IsReturnTypeStaticAbstractInterface)
            {
                writer.AppendLine($"get => ({prop.ReturnType})_engine.HandleCallWithReturn<object?>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), null)!;");
            }
            else if (prop.IsAbstractMember)
            {
                writer.AppendLine($"get => _engine.HandleCallWithReturn<{prop.ReturnType}>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), {prop.SmartDefault});");
            }
            else if (prop.IsReturnTypeStaticAbstractInterface)
            {
                // Virtual property getter: try engine, fall back to base (CS8920-safe)
                writer.AppendLine("get");
                writer.OpenBrace();
                writer.AppendLine($"if (_engine.TryHandleCallWithReturn<object?>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), null, out var __rawResult))");
                writer.AppendLine("{");
                writer.IncreaseIndent();
                writer.AppendLine($"return ({prop.ReturnType})__rawResult!;");
                writer.DecreaseIndent();
                writer.AppendLine("}");
                writer.AppendLine($"return base.{prop.Name};");
                writer.CloseBrace();
            }
            else
            {
                // Virtual property getter: try engine, fall back to base
                writer.AppendLine("get");
                writer.OpenBrace();
                writer.AppendLine($"if (_engine.TryHandleCallWithReturn<{prop.ReturnType}>({prop.MemberId}, \"get_{prop.Name}\", global::System.Array.Empty<object?>(), {prop.SmartDefault}, out var __result))");
                writer.AppendLine("{");
                writer.IncreaseIndent();
                writer.AppendLine("return __result;");
                writer.DecreaseIndent();
                writer.AppendLine("}");
                writer.AppendLine($"return base.{prop.Name};");
                writer.CloseBrace();
            }
        }

        if (prop.HasSetter)
        {
            var setterArgs = prop.IsRefStructReturn
                ? "global::System.Array.Empty<object?>()"
                : "new object?[] { value }";

            if (prop.IsAbstractMember)
            {
                writer.AppendLine($"set => _engine.HandleCall({prop.SetterMemberId}, \"set_{prop.Name}\", {setterArgs});");
            }
            else
            {
                // Virtual property setter: try engine, fall back to base
                writer.AppendLine("set");
                writer.OpenBrace();
                writer.AppendLine($"if (!_engine.TryHandleCall({prop.SetterMemberId}, \"set_{prop.Name}\", {setterArgs}))");
                writer.AppendLine("{");
                writer.IncreaseIndent();
                writer.AppendLine($"base.{prop.Name} = value;");
                writer.DecreaseIndent();
                writer.AppendLine("}");
                writer.CloseBrace();
            }
        }

        writer.CloseBrace();
    }

    private static void GenerateEvent(CodeWriter writer, MockEventModel evt)
    {
        // Backing delegate field
        writer.AppendLine($"private {evt.EventHandlerType}? _backing_{evt.Name};");
        writer.AppendLine();

        // Event add/remove accessors
        writer.AppendLine($"public event {evt.EventHandlerType}? {evt.Name}");
        writer.OpenBrace();
        writer.AppendLine($"add {{ _backing_{evt.Name} += value; _engine.RecordEventSubscription(\"{evt.Name}\", true); }}");
        writer.AppendLine($"remove {{ _backing_{evt.Name} -= value; _engine.RecordEventSubscription(\"{evt.Name}\", false); }}");
        writer.CloseBrace();
        writer.AppendLine();

        // Raise method for generated code to call
        writer.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        var raiseParams = evt.RaiseParameterList.Length == 0
            ? ""
            : string.Join(", ", evt.RaiseParameterList.Select(p => $"{p.FullyQualifiedType} {p.Name}"));
        var invokeArgs = string.IsNullOrEmpty(evt.InvokeArgs) ? "" : evt.InvokeArgs;
        using (writer.Block($"internal void Raise_{evt.Name}({raiseParams})"))
        {
            if (string.IsNullOrEmpty(invokeArgs))
            {
                writer.AppendLine($"_backing_{evt.Name}?.Invoke();");
            }
            else
            {
                writer.AppendLine($"_backing_{evt.Name}?.Invoke({invokeArgs});");
            }
        }
    }

    private static void GeneratePartialEvent(CodeWriter writer, MockEventModel evt)
    {
        // Backing delegate field
        writer.AppendLine($"private {evt.EventHandlerType}? _backing_{evt.Name};");
        writer.AppendLine();

        // Event add/remove accessors with override
        writer.AppendLine($"public override event {evt.EventHandlerType}? {evt.Name}");
        writer.OpenBrace();
        writer.AppendLine($"add {{ _backing_{evt.Name} += value; _engine.RecordEventSubscription(\"{evt.Name}\", true); }}");
        writer.AppendLine($"remove {{ _backing_{evt.Name} -= value; _engine.RecordEventSubscription(\"{evt.Name}\", false); }}");
        writer.CloseBrace();
        writer.AppendLine();

        // Raise method for generated code to call
        writer.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        var raiseParams = evt.RaiseParameterList.Length == 0
            ? ""
            : string.Join(", ", evt.RaiseParameterList.Select(p => $"{p.FullyQualifiedType} {p.Name}"));
        var invokeArgs = string.IsNullOrEmpty(evt.InvokeArgs) ? "" : evt.InvokeArgs;
        using (writer.Block($"internal void Raise_{evt.Name}({raiseParams})"))
        {
            if (string.IsNullOrEmpty(invokeArgs))
            {
                writer.AppendLine($"_backing_{evt.Name}?.Invoke();");
            }
            else
            {
                writer.AppendLine($"_backing_{evt.Name}?.Invoke({invokeArgs});");
            }
        }
    }

    private static void GenerateRaiseEventDispatch(CodeWriter writer, MockTypeModel model)
    {
        writer.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        using (writer.Block("public void RaiseEvent(string eventName, object? args)"))
        {
            var instanceEvents = model.Events.Where(e => !e.IsStaticAbstract).ToArray();
            if (instanceEvents.Length == 0)
            {
                writer.AppendLine("throw new global::System.InvalidOperationException($\"No event named '{eventName}' exists on this mock.\");");
            }
            else
            {
                using (writer.Block("switch (eventName)"))
                {
                    foreach (var evt in instanceEvents)
                    {
                        writer.AppendLine($"case \"{evt.Name}\":");
                        writer.IncreaseIndent();

                        // Determine how to invoke: if the event handler has parameters matching EventArgs, pass args
                        if (evt.RaiseParameterList.Length == 0)
                        {
                            // No-parameter event (e.g., Action)
                            writer.AppendLine($"Raise_{evt.Name}();");
                        }
                        else if (evt.RaiseParameterList.Length > 1)
                        {
                            // Multi-parameter delegate — cast args to object[] and spread
                            writer.AppendLine("if (args is object?[] __argArray)");
                            writer.AppendLine("{");
                            writer.IncreaseIndent();

                            var castArgs = new List<string>();
                            for (int i = 0; i < evt.RaiseParameterList.Length; i++)
                            {
                                castArgs.Add($"({evt.RaiseParameterList[i].FullyQualifiedType})__argArray[{i}]");
                            }
                            writer.AppendLine($"Raise_{evt.Name}({string.Join(", ", castArgs)});");
                            writer.DecreaseIndent();
                            writer.AppendLine("}");
                            writer.AppendLine("else");
                            writer.AppendLine("{");
                            writer.IncreaseIndent();
                            writer.AppendLine($"throw new global::System.ArgumentException($\"Event '{evt.Name}' requires an object[] of arguments.\");");
                            writer.DecreaseIndent();
                            writer.AppendLine("}");
                        }
                        else
                        {
                            // Single-parameter event (e.g., EventHandler<TArgs>)
                            writer.AppendLine($"Raise_{evt.Name}(({evt.RaiseParameterList[0].FullyQualifiedType})args!);");
                        }

                        writer.AppendLine("break;");
                        writer.DecreaseIndent();
                    }

                    writer.AppendLine("default:");
                    writer.IncreaseIndent();
                    writer.AppendLine("throw new global::System.InvalidOperationException($\"No event named '{eventName}' exists on this mock.\");");
                    writer.DecreaseIndent();
                }
            }
        }
    }

    internal static string GetParameterList(MockMemberModel method) =>
        FormatParameterList(method.Parameters);

    private static string FormatParameterList(EquatableArray<MockParameterModel> parameters)
    {
        return string.Join(", ", parameters.Select(p =>
        {
            var direction = p.Direction switch
            {
                ParameterDirection.Out => "out ",
                ParameterDirection.Ref => "ref ",
                ParameterDirection.In_Readonly => "in ",
                _ => ""
            };
            return $"{direction}{p.FullyQualifiedType} {p.Name}";
        }));
    }

    internal static string GetTypeParameterList(MockMemberModel method) =>
        FormatTypeParameterList(method.TypeParameters);

    private static string FormatTypeParameterList(EquatableArray<MockTypeParameterModel> typeParameters)
    {
        if (typeParameters.Length == 0) return "";
        return "<" + string.Join(", ", typeParameters.Select(tp => tp.Name)) + ">";
    }

    // Only for non-override declarations (interface impls, extension methods).
    // C# prohibits restating constraints on override methods (CS0460).
    internal static string GetConstraintClauses(MockMemberModel method, bool forExplicitImplementation = false) =>
        FormatConstraintClauses(method.TypeParameters, forExplicitImplementation);

    private static string FormatConstraintClauses(EquatableArray<MockTypeParameterModel> typeParameters, bool forExplicitImplementation = false)
    {
        var clauses = new List<string>();
        foreach (var tp in typeParameters)
        {
            if (!string.IsNullOrEmpty(tp.Constraints))
            {
                clauses.Add($"where {tp.Name} : {tp.Constraints}");
            }
            else if (forExplicitImplementation && tp.HasAnnotatedNullableUsage)
            {
                clauses.Add($"where {tp.Name} : default");
            }
        }
        return clauses.Count > 0 ? " " + string.Join(' ', clauses) : "";
    }

    /// <summary>
    /// Computes dispatch strategy for a method: typed (arity 1-8, no ref structs) or fallback (object?[]).
    /// </summary>
    private static (bool IsTyped, string? TypeArgs, string? ArgsList) GetTypedDispatchInfo(MockMemberModel method)
    {
        if (method.HasRefStructParams) return (false, null, null);
        var nonOutParams = method.Parameters.Where(p => p.Direction != ParameterDirection.Out).ToList();
        if (nonOutParams.Count is < 1 or > 8) return (false, null, null);
        var typeArgs = string.Join(", ", nonOutParams.Select(p => p.FullyQualifiedType));
        var argsList = string.Join(", ", nonOutParams.Select(p => p.Name));
        return (true, typeArgs, argsList);
    }

    /// <summary>Emits a HandleCall or TryHandleCall invocation, choosing typed or fallback path.</summary>
    private static string EmitHandleCall(bool isTyped, string? typeArgs, string? argsList, string? argsArray, int memberId, string memberName)
        => isTyped
            ? $"_engine.HandleCall<{typeArgs}>({memberId}, \"{memberName}\", {argsList})"
            : $"_engine.HandleCall({memberId}, \"{memberName}\", {argsArray})";

    /// <summary>Emits a HandleCallWithReturn invocation, choosing typed or fallback path.</summary>
    private static string EmitHandleCallWithReturn(bool isTyped, string? typeArgs, string? argsList, string? argsArray, string returnTypeArg, int memberId, string memberName, string defaultValue)
        => isTyped
            ? $"_engine.HandleCallWithReturn<{returnTypeArg}, {typeArgs}>({memberId}, \"{memberName}\", {argsList}, {defaultValue})"
            : $"_engine.HandleCallWithReturn<{returnTypeArg}>({memberId}, \"{memberName}\", {argsArray}, {defaultValue})";

    /// <summary>Emits a TryHandleCall condition, choosing typed or fallback path.</summary>
    private static string EmitTryHandleCall(bool isTyped, string? typeArgs, string? argsList, string? argsArray, int memberId, string memberName)
        => isTyped
            ? $"_engine.TryHandleCall<{typeArgs}>({memberId}, \"{memberName}\", {argsList})"
            : $"_engine.TryHandleCall({memberId}, \"{memberName}\", {argsArray})";

    /// <summary>Emits a TryHandleCallWithReturn condition, choosing typed or fallback path.</summary>
    private static string EmitTryHandleCallWithReturn(bool isTyped, string? typeArgs, string? argsList, string? argsArray, string returnTypeArg, int memberId, string memberName, string defaultValue, string outVar)
        => isTyped
            ? $"_engine.TryHandleCallWithReturn<{returnTypeArg}, {typeArgs}>({memberId}, \"{memberName}\", {argsList}, {defaultValue}, out var {outVar})"
            : $"_engine.TryHandleCallWithReturn<{returnTypeArg}>({memberId}, \"{memberName}\", {argsArray}, {defaultValue}, out var {outVar})";

    /// <summary>
    /// Returns true if the method has any out or ref parameters that need read-back.
    /// </summary>
    private static bool HasOutRefParams(MockMemberModel method)
    {
        return method.Parameters.Any(p => p.Direction == ParameterDirection.Out || p.Direction == ParameterDirection.Ref);
    }

    /// <summary>
    /// Emits code to read back out/ref parameter values from OutRefContext after an engine call.
    /// </summary>
    internal static void EmitOutRefReadback(CodeWriter writer, MockMemberModel method)
    {
        if (!HasOutRefParams(method)) return;

        writer.AppendLine("var __outRef = global::TUnit.Mocks.Setup.OutRefContext.Consume();");
        using (writer.Block("if (__outRef is not null)"))
        {
            EmitOutRefParamAssignments(writer, method);
        }
    }

    /// <summary>
    /// For async methods: emits code to check <see cref="TUnit.Mocks.Setup.RawReturnContext"/>
    /// and return the raw Task/ValueTask directly if one was set by a <c>ReturnsAsync</c> setup.
    /// </summary>
    private static void EmitRawReturnCheck(CodeWriter writer, MockMemberModel method)
    {
        if (!method.IsAsync) return;

        // IMPORTANT: This check must appear synchronously (no await) after the engine
        // dispatch call. The [ThreadStatic] RawReturnContext requires same-thread consumption.
        writer.AppendLine($"if (global::TUnit.Mocks.Setup.RawReturnContext.TryConsume(out var __rawAsync))");
        writer.OpenBrace();
        writer.AppendLine($"if (__rawAsync is {method.ReturnType} __typedAsync) return __typedAsync;");
        writer.AppendLine($"throw new global::System.InvalidOperationException($\"ReturnsAsync: expected {method.ReturnType} but got {{__rawAsync?.GetType().Name ?? \"null\"}}\");");
        writer.CloseBrace();
    }

    /// <summary>
    /// For ref struct return methods with span support: emits code to consume OutRefContext,
    /// read back out/ref params, extract span return value, and return.
    /// Always ends with "return default;" as fallback.
    /// </summary>
    private static void EmitSpanReturnReadback(CodeWriter writer, MockMemberModel method)
    {
        writer.AppendLine("var __outRef = global::TUnit.Mocks.Setup.OutRefContext.Consume();");
        using (writer.Block("if (__outRef is not null)"))
        {
            EmitOutRefParamAssignments(writer, method);
            writer.AppendLine($"if (__outRef.TryGetValue(global::TUnit.Mocks.Setup.OutRefContext.SpanReturnValueIndex, out var __spanRet)) return new {method.ReturnType}(({method.SpanReturnElementType}[])__spanRet!);");
        }
        writer.AppendLine("return default;");
    }

    /// <summary>
    /// Emits individual out/ref parameter assignments from the __outRef dictionary.
    /// Shared by <see cref="EmitOutRefReadback"/> and <see cref="EmitSpanReturnReadback"/>.
    /// </summary>
    private static void EmitOutRefParamAssignments(CodeWriter writer, MockMemberModel method)
    {
        for (int i = 0; i < method.Parameters.Length; i++)
        {
            var p = method.Parameters[i];
            if (p.IsRefStruct && p.SpanElementType is null) continue; // non-span ref structs can't be cast from object
            if (p.Direction == ParameterDirection.Out || p.Direction == ParameterDirection.Ref)
            {
                if (p.SpanElementType is not null)
                {
                    // Span types: reconstruct from stored array
                    writer.AppendLine($"if (__outRef.TryGetValue({i}, out var __v{i})) {p.Name} = new {p.FullyQualifiedType}(({p.SpanElementType}[])__v{i}!);");
                }
                else
                {
                    writer.AppendLine($"if (__outRef.TryGetValue({i}, out var __v{i})) {p.Name} = ({p.FullyQualifiedType})__v{i}!;");
                }
            }
        }
    }

    internal static string EmitArgsArrayVariable(CodeWriter writer, MockMemberModel method)
    {
        if (!method.HasRefStructParams)
        {
            return GetArgsArrayExpression(method, false);
        }

        writer.AppendLine("#if NET9_0_OR_GREATER");
        writer.AppendLine($"var __args = {GetArgsArrayExpression(method, true)};");
        writer.AppendLine("#else");
        writer.AppendLine($"var __args = {GetArgsArrayExpression(method, false)};");
        writer.AppendLine("#endif");
        return "__args";
    }

    private static string GetArgsArrayExpression(MockMemberModel method, bool includeRefStructSentinels)
    {
        var nonOutParams = method.Parameters.Where(p => p.Direction != ParameterDirection.Out).ToList();
        if (includeRefStructSentinels)
        {
            if (nonOutParams.Count == 0) return "global::System.Array.Empty<object?>()";
            var args = string.Join(", ", nonOutParams.Select(p => p.IsRefStruct ? "null" : p.Name));
            return $"new object?[] {{ {args} }}";
        }
        var matchableParams = nonOutParams.Where(p => !p.IsRefStruct).ToList();
        if (matchableParams.Count == 0) return "global::System.Array.Empty<object?>()";
        var argsStr = string.Join(", ", matchableParams.Select(p => p.Name));
        return $"new object?[] {{ {argsStr} }}";
    }

    /// <summary>
    /// Gets the argument pass-through list for calling base.Method(), preserving ref/out/in directions.
    /// </summary>
    internal static string GetArgPassList(MockMemberModel method)
    {
        return string.Join(", ", method.Parameters.Select(p =>
        {
            var direction = p.Direction switch
            {
                ParameterDirection.Out => "out ",
                ParameterDirection.Ref => "ref ",
                ParameterDirection.In_Readonly => "in ",
                _ => ""
            };
            return $"{direction}{p.Name}";
        }));
    }

    public static string GetSafeName(string typeName)
    {
        return SanitizeIdentifier(typeName);
    }

    /// <summary>
    /// Gets a safe name that includes additional interfaces for multi-interface mocks.
    /// </summary>
    public static string GetCompositeSafeName(MockTypeModel model)
    {
        var name = model.FullyQualifiedName;
        if (model.AdditionalInterfaceNames.Length > 0)
        {
            name += "_" + string.Join('_', model.AdditionalInterfaceNames);
        }
        return GetSafeName(name);
    }

    /// <summary>
    /// Gets a short safe name derived from just the type name (without namespace),
    /// sanitized for generic type arguments. Produces readable names like
    /// "IGreeter_" instead of "MyApp_IGreeter_" and "IFoo_SomeEnum_" instead of
    /// "IFoo_Sandbox_SomeEnum_" when the type argument shares the outer namespace.
    /// </summary>
    public static string GetShortSafeName(MockTypeModel model)
    {
        var name = StripGlobalPrefix(model.FullyQualifiedName);
        var hasNamespace = !IsGlobalNamespace(model.Namespace);

        if (hasNamespace && name.StartsWith(model.Namespace + "."))
            name = name.Substring(model.Namespace.Length + 1);

        // Strip same-namespace qualifications from generic type arguments so that
        // IFoo<global::Sandbox.SomeEnum> becomes IFoo<SomeEnum> (not IFoo<Sandbox.SomeEnum>).
        // Cross-namespace args are kept for disambiguation.
        if (hasNamespace)
            name = name.Replace("global::" + model.Namespace + ".", "");

        return SanitizeIdentifier(name);
    }

    /// <summary>
    /// Gets a composite short safe name that includes additional interfaces for multi-interface mocks.
    /// For single-type mocks, identical to GetShortSafeName.
    /// </summary>
    public static string GetCompositeShortSafeName(MockTypeModel model)
    {
        var name = GetShortSafeName(model);
        if (model.AdditionalInterfaceNames.Length > 0)
        {
            name += "_" + string.Join("_", model.AdditionalInterfaceNames.Select(StripNamespaceFromFqn));
        }
        return name;
    }

    /// <summary>
    /// Strips the global:: prefix and namespace from a fully qualified name,
    /// returning just the type name (sanitized for use in identifiers).
    /// </summary>
    private static string StripNamespaceFromFqn(string fqn)
    {
        var name = StripGlobalPrefix(fqn);

        // Find last dot not inside angle brackets to handle generic type arguments
        var lastDotIndex = -1;
        var depth = 0;
        for (int i = 0; i < name.Length; i++)
        {
            var c = name[i];
            if (c == '<') depth++;
            else if (c == '>') depth--;
            else if (c == '.' && depth == 0) lastDotIndex = i;
        }
        if (lastDotIndex >= 0)
            name = name.Substring(lastDotIndex + 1);

        return SanitizeIdentifier(name);
    }

    private static string StripGlobalPrefix(string name)
        => name.StartsWith("global::") ? name.Substring("global::".Length) : name;

    private static string SanitizeIdentifier(string name)
    {
        name = name.Replace("global::", "");

        var sb = new System.Text.StringBuilder(name.Length);
        var lastWasUnderscore = false;

        foreach (var c in name)
        {
            if (c == ' ')
                continue;

            if (char.IsLetterOrDigit(c) || c == '_')
            {
                if (c == '_')
                {
                    if (lastWasUnderscore)
                        continue;
                    lastWasUnderscore = true;
                }
                else
                {
                    lastWasUnderscore = false;
                }
                sb.Append(c);
            }
            else if (!lastWasUnderscore)
            {
                sb.Append('_');
                lastWasUnderscore = true;
            }
        }

        return sb.ToString();
    }

    private static bool IsGlobalNamespace(string ns)
        => string.IsNullOrEmpty(ns) || ns == "<global namespace>";

    /// <summary>
    /// Gets the generated namespace for mock types.
    /// Types in the global namespace go to TUnit.Mocks.Generated;
    /// namespaced types go to TUnit.Mocks.Generated.{OriginalNamespace}.
    /// </summary>
    public static string GetMockNamespace(MockTypeModel model)
    {
        return IsGlobalNamespace(model.Namespace)
            ? "TUnit.Mocks.Generated"
            : $"TUnit.Mocks.Generated.{model.Namespace}";
    }

    /// <summary>
    /// Gets the fully qualified type name to use as a generic type argument.
    /// For types with static abstract members, returns the bridge interface FQN
    /// (which resolves CS8920 by providing DIMs for all static abstract members).
    /// For other types, returns the original FQN.
    /// </summary>
    public static string GetMockableTypeName(MockTypeModel model)
    {
        if (!model.HasStaticAbstractMembers) return model.FullyQualifiedName;
        var shortName = GetCompositeShortSafeName(model);
        var ns = GetMockNamespace(model);
        return $"global::{ns}.{shortName}Mockable";
    }

    /// <summary>
    /// Emits the static engine assignment with a guard that detects multiple mocks of the same
    /// static-abstract interface type within a single test context.
    /// </summary>
    private static void EmitIMockObjectProperty(CodeWriter writer)
    {
        writer.AppendLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
        writer.AppendLine("global::TUnit.Mocks.IMock? global::TUnit.Mocks.IMockObject.MockWrapper { get; set; }");
        writer.AppendLine();
    }

    internal static void EmitStaticEngineAssignment(CodeWriter writer, string safeName)
    {
        writer.AppendLine($"if ({safeName}StaticEngine.Engine is not null)");
        writer.OpenBrace();
        writer.AppendLine($"throw new global::System.InvalidOperationException(");
        writer.AppendLine($"    \"Multiple mocks of an interface with static abstract members cannot be created in the same test context. \" +");
        writer.AppendLine($"    \"Static member calls are routed via a shared AsyncLocal engine, so only one mock instance per type is supported per test.\");");
        writer.CloseBrace();
        writer.AppendLine($"{safeName}StaticEngine.Engine = engine;");
    }

}
