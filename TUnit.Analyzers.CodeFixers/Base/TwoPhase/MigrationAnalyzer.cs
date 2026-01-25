using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace TUnit.Analyzers.CodeFixers.Base.TwoPhase;

/// <summary>
/// Phase 1: Analyzes source code while semantic model is valid.
/// Collects all conversion targets into a ConversionPlan.
/// Derived classes implement framework-specific analysis.
///
/// CRITICAL: All semantic model queries MUST happen on the original root.
/// Annotations are added AFTER all semantic analysis is complete.
/// </summary>
public abstract class MigrationAnalyzer
{
    protected SemanticModel SemanticModel { get; }
    protected Compilation Compilation { get; }
    protected ConversionPlan Plan { get; }

    /// <summary>
    /// The original, unmodified compilation unit for semantic model queries.
    /// </summary>
    private CompilationUnitSyntax _originalRoot = null!;

    /// <summary>
    /// Methods that implement interface members (collected during pre-analysis).
    /// These should NOT have their signatures changed to async.
    /// </summary>
    private HashSet<TextSpan> _interfaceImplementingMethods = new();

    protected MigrationAnalyzer(SemanticModel semanticModel, Compilation compilation)
    {
        SemanticModel = semanticModel;
        Compilation = compilation;
        Plan = new ConversionPlan();
    }

    /// <summary>
    /// Analyzes the compilation unit and returns a conversion plan.
    /// The returned syntax tree has annotations added to mark conversion targets.
    /// </summary>
    public (CompilationUnitSyntax AnnotatedRoot, ConversionPlan Plan) Analyze(CompilationUnitSyntax root)
    {
        _originalRoot = root;

        // === PRE-ANALYSIS PHASE ===
        // Collect all semantic information BEFORE any tree modifications.
        // The semantic model only works on the original tree.

        // Collect interface-implementing methods (needed for async conversion decision)
        CollectInterfaceImplementingMethods();

        // === ANALYSIS PHASE ===
        // Now analyze and annotate. Use span-based lookup when semantic info is needed.
        var annotatedRoot = root;

        // 1. Analyze assertions (uses semantic model on original nodes)
        annotatedRoot = AnalyzeAssertions(annotatedRoot);

        // 2. Analyze attributes
        annotatedRoot = AnalyzeAttributes(annotatedRoot);

        // 2b. Analyze parameter attributes (e.g., [Range])
        annotatedRoot = AnalyzeParameterAttributes(annotatedRoot);

        // 2c. Analyze methods for missing attributes (e.g., add [Test] when only [TestCase])
        annotatedRoot = AnalyzeMethodsForMissingAttributes(annotatedRoot);

        // 3. Analyze base types
        annotatedRoot = AnalyzeBaseTypes(annotatedRoot);

        // 4. Analyze members (fields, properties for removal)
        annotatedRoot = AnalyzeMembers(annotatedRoot);

        // 5. Analyze constructor parameters
        annotatedRoot = AnalyzeConstructorParameters(annotatedRoot);

        // 6. Analyze special invocations (e.g., Record.Exception, ITestOutputHelper)
        annotatedRoot = AnalyzeSpecialInvocations(annotatedRoot);

        // 7. Analyze TheoryData fields/properties
        annotatedRoot = AnalyzeTheoryData(annotatedRoot);

        // 8. Analyze method signatures (uses pre-collected interface info)
        annotatedRoot = AnalyzeMethodSignatures(annotatedRoot);

        // 9. Determine usings to add/remove
        AnalyzeUsings();

        return (annotatedRoot, Plan);
    }

    /// <summary>
    /// Pre-analysis: Collects all interface-implementing methods from the original tree.
    /// This must be done before any tree modifications.
    /// </summary>
    private void CollectInterfaceImplementingMethods()
    {
        foreach (var method in _originalRoot.DescendantNodes().OfType<MethodDeclarationSyntax>())
        {
            try
            {
                var methodSymbol = SemanticModel.GetDeclaredSymbol(method);
                if (methodSymbol == null) continue;

                var containingType = methodSymbol.ContainingType;
                if (containingType == null) continue;

                foreach (var iface in containingType.AllInterfaces)
                {
                    foreach (var member in iface.GetMembers().OfType<IMethodSymbol>())
                    {
                        var impl = containingType.FindImplementationForInterfaceMember(member);
                        if (SymbolEqualityComparer.Default.Equals(impl, methodSymbol))
                        {
                            _interfaceImplementingMethods.Add(method.Span);
                            break;
                        }
                    }
                }
            }
            catch
            {
                // Ignore errors in pre-analysis
            }
        }
    }

    /// <summary>
    /// Checks if a method at the given span implements an interface method.
    /// Uses pre-collected data to avoid semantic model queries on modified tree.
    /// </summary>
    protected bool IsInterfaceImplementation(TextSpan methodSpan)
    {
        return _interfaceImplementingMethods.Contains(methodSpan);
    }

    /// <summary>
    /// Analyzes assertions and adds them to the plan.
    /// Returns the root with annotations added to assertion nodes.
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeAssertions(CompilationUnitSyntax root)
    {
        // Find assertions on the ORIGINAL tree (for semantic analysis)
        var assertionNodes = FindAssertionNodes(_originalRoot).ToList();
        var currentRoot = root;

        foreach (var originalNode in assertionNodes)
        {
            try
            {
                var conversion = AnalyzeAssertion(originalNode);
                if (conversion != null)
                {
                    // Check if the containing method has ref/out parameters
                    // If so, we can't make it async, so use .Wait() instead of await
                    if (conversion.IntroducesAwait && ContainingMethodHasRefOrOutParameters(originalNode))
                    {
                        // Create a new conversion with .Wait() instead of await
                        var newReplacementCode = conversion.ReplacementCode;
                        if (newReplacementCode.StartsWith("await "))
                        {
                            newReplacementCode = newReplacementCode.Substring(6) + ".Wait()";
                        }
                        conversion = new AssertionConversion
                        {
                            Kind = conversion.Kind,
                            OriginalText = conversion.OriginalText,
                            ReplacementCode = newReplacementCode,
                            IntroducesAwait = false,
                            TodoComment = conversion.TodoComment
                        };
                    }

                    Plan.Assertions.Add(conversion);

                    // Find the corresponding node in the current tree by span
                    var nodeToAnnotate = currentRoot.DescendantNodes()
                        .OfType<InvocationExpressionSyntax>()
                        .FirstOrDefault(n => n.Span == originalNode.Span);

                    if (nodeToAnnotate != null)
                    {
                        var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(conversion.Annotation);
                        currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "AssertionAnalysis",
                    Description = ex.Message,
                    OriginalCode = originalNode.ToString(),
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    /// <summary>
    /// Checks if the method containing this node has ref or out parameters.
    /// Methods with ref/out parameters cannot be made async, so assertions must use .Wait() instead of await.
    /// </summary>
    private static bool ContainingMethodHasRefOrOutParameters(SyntaxNode node)
    {
        var containingMethod = node.Ancestors()
            .OfType<MethodDeclarationSyntax>()
            .FirstOrDefault();

        if (containingMethod == null)
            return false;

        return containingMethod.ParameterList.Parameters
            .Any(p => p.Modifiers.Any(m => m.IsKind(SyntaxKind.RefKeyword) || m.IsKind(SyntaxKind.OutKeyword)));
    }

    /// <summary>
    /// Finds all assertion invocation nodes in the syntax tree.
    /// Called on the ORIGINAL tree for semantic model compatibility.
    /// </summary>
    protected abstract IEnumerable<InvocationExpressionSyntax> FindAssertionNodes(CompilationUnitSyntax root);

    /// <summary>
    /// Analyzes a single assertion and returns the conversion info.
    /// Returns null if this node should not be converted.
    /// Called with nodes from the ORIGINAL tree.
    /// </summary>
    protected abstract AssertionConversion? AnalyzeAssertion(InvocationExpressionSyntax node);

    /// <summary>
    /// Analyzes attributes and adds them to the plan.
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeAttributes(CompilationUnitSyntax root)
    {
        var attributeNodes = _originalRoot.DescendantNodes().OfType<AttributeSyntax>().ToList();
        var currentRoot = root;

        foreach (var originalNode in attributeNodes)
        {
            try
            {
                // Check for removal first
                if (ShouldRemoveAttribute(originalNode))
                {
                    var removal = new AttributeRemoval { OriginalText = originalNode.ToString() };
                    Plan.AttributeRemovals.Add(removal);

                    var nodeToAnnotate = currentRoot.DescendantNodes()
                        .OfType<AttributeSyntax>()
                        .FirstOrDefault(n => n.Span == originalNode.Span);

                    if (nodeToAnnotate != null)
                    {
                        var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(removal.Annotation);
                        currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                    }
                    continue;
                }

                // Check for conversion
                var conversion = AnalyzeAttribute(originalNode);
                if (conversion != null)
                {
                    Plan.Attributes.Add(conversion);

                    var nodeToAnnotate = currentRoot.DescendantNodes()
                        .OfType<AttributeSyntax>()
                        .FirstOrDefault(n => n.Span == originalNode.Span);

                    if (nodeToAnnotate != null)
                    {
                        var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(conversion.Annotation);
                        currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                    }
                }
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "AttributeAnalysis",
                    Description = ex.Message,
                    OriginalCode = originalNode.ToString(),
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    /// <summary>
    /// Returns true if this attribute should be removed entirely.
    /// </summary>
    protected abstract bool ShouldRemoveAttribute(AttributeSyntax node);

    /// <summary>
    /// Analyzes a single attribute and returns the conversion info.
    /// Returns null if this attribute should not be converted.
    /// </summary>
    protected abstract AttributeConversion? AnalyzeAttribute(AttributeSyntax node);

    /// <summary>
    /// Analyzes parameter attributes (e.g., [Range] on method parameters).
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeParameterAttributes(CompilationUnitSyntax root)
    {
        var parameterNodes = _originalRoot.DescendantNodes().OfType<ParameterSyntax>().ToList();
        var currentRoot = root;

        foreach (var parameter in parameterNodes)
        {
            if (parameter.AttributeLists.Count == 0) continue;

            foreach (var attributeList in parameter.AttributeLists)
            {
                foreach (var originalAttr in attributeList.Attributes)
                {
                    try
                    {
                        var conversion = AnalyzeParameterAttribute(originalAttr, parameter);
                        if (conversion != null)
                        {
                            Plan.ParameterAttributes.Add(conversion);

                            var nodeToAnnotate = currentRoot.DescendantNodes()
                                .OfType<AttributeSyntax>()
                                .FirstOrDefault(n => n.Span == originalAttr.Span);

                            if (nodeToAnnotate != null)
                            {
                                var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(conversion.Annotation);
                                currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Plan.Failures.Add(new ConversionFailure
                        {
                            Phase = "ParameterAttributeAnalysis",
                            Description = ex.Message,
                            OriginalCode = originalAttr.ToString(),
                            Exception = ex
                        });
                    }
                }
            }
        }

        return currentRoot;
    }

    /// <summary>
    /// Analyzes a single parameter attribute and returns the conversion info.
    /// Returns null if this attribute should not be converted.
    /// </summary>
    protected virtual ParameterAttributeConversion? AnalyzeParameterAttribute(AttributeSyntax attr, ParameterSyntax parameter)
    {
        return null; // Default: no parameter attribute conversion
    }

    /// <summary>
    /// Analyzes methods to add missing attributes (e.g., add [Test] when only [TestCase]).
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeMethodsForMissingAttributes(CompilationUnitSyntax root)
    {
        return root; // Default: no missing attribute additions
    }

    /// <summary>
    /// Analyzes base types and adds removals to the plan.
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeBaseTypes(CompilationUnitSyntax root)
    {
        var classNodes = _originalRoot.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
        var currentRoot = root;

        foreach (var classNode in classNodes)
        {
            if (classNode.BaseList == null) continue;

            foreach (var originalBaseType in classNode.BaseList.Types)
            {
                try
                {
                    if (ShouldRemoveBaseType(originalBaseType))
                    {
                        var removal = new BaseTypeRemoval
                        {
                            TypeName = originalBaseType.Type.ToString(),
                            OriginalText = originalBaseType.ToString()
                        };
                        Plan.BaseTypeRemovals.Add(removal);

                        var nodeToAnnotate = currentRoot.DescendantNodes()
                            .OfType<BaseTypeSyntax>()
                            .FirstOrDefault(n => n.Span == originalBaseType.Span);

                        if (nodeToAnnotate != null)
                        {
                            var annotatedNode = nodeToAnnotate.WithAdditionalAnnotations(removal.Annotation);
                            currentRoot = currentRoot.ReplaceNode(nodeToAnnotate, annotatedNode);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Plan.Failures.Add(new ConversionFailure
                    {
                        Phase = "BaseTypeAnalysis",
                        Description = ex.Message,
                        OriginalCode = originalBaseType.ToString(),
                        Exception = ex
                    });
                }
            }
        }

        return currentRoot;
    }

    /// <summary>
    /// Returns true if this base type should be removed.
    /// </summary>
    protected abstract bool ShouldRemoveBaseType(BaseTypeSyntax baseType);

    /// <summary>
    /// Analyzes members (fields, properties) for removal.
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeMembers(CompilationUnitSyntax root)
    {
        // Default: no member removals. Override in derived classes.
        return root;
    }

    /// <summary>
    /// Analyzes constructor parameters for removal.
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeConstructorParameters(CompilationUnitSyntax root)
    {
        // Default: no parameter removals. Override in derived classes.
        return root;
    }

    /// <summary>
    /// Analyzes special invocations (e.g., Record.Exception, ITestOutputHelper.WriteLine).
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeSpecialInvocations(CompilationUnitSyntax root)
    {
        // Default: no special invocations. Override in derived classes.
        return root;
    }

    /// <summary>
    /// Analyzes TheoryData fields/properties for conversion to IEnumerable.
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeTheoryData(CompilationUnitSyntax root)
    {
        // Default: no TheoryData conversions. Override in derived classes.
        return root;
    }

    /// <summary>
    /// Analyzes method signatures for async conversion.
    /// This should be called after assertion analysis to know which methods will have awaits.
    /// </summary>
    protected virtual CompilationUnitSyntax AnalyzeMethodSignatures(CompilationUnitSyntax root)
    {
        // Find methods that will contain await after transformation
        // We need to map from annotated nodes back to original spans
        var methodSpansWithAwaits = new HashSet<TextSpan>();

        // Check which methods contain assertions that introduce await
        foreach (var assertion in Plan.Assertions.Where(a => a.IntroducesAwait))
        {
            // Find the annotated node in the current tree
            var assertionNode = root.DescendantNodes()
                .FirstOrDefault(n => n.HasAnnotation(assertion.Annotation));

            if (assertionNode != null)
            {
                // Find the containing method
                var containingMethod = assertionNode.Ancestors()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault();

                if (containingMethod != null)
                {
                    // Use the original span (before annotation was added)
                    // Since annotations don't change spans, this works
                    methodSpansWithAwaits.Add(containingMethod.Span);
                }
            }
        }

        var currentRoot = root;

        // Process each method that needs async
        foreach (var methodSpan in methodSpansWithAwaits)
        {
            var method = currentRoot.DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .FirstOrDefault(m => m.Span == methodSpan);

            if (method == null) continue;

            // Skip if already async
            if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                continue;

            // Skip interface implementations (uses pre-collected data)
            if (IsInterfaceImplementation(methodSpan))
                continue;

            try
            {
                var returnTypeText = method.ReturnType.ToString();

                // Determine what return type change is needed
                var (changeReturnTypeToTask, wrapReturnTypeInTask, originalReturnType) = AnalyzeReturnTypeForAsync(returnTypeText);

                var change = new MethodSignatureChange
                {
                    AddAsync = true,
                    ChangeReturnTypeToTask = changeReturnTypeToTask,
                    WrapReturnTypeInTask = wrapReturnTypeInTask,
                    OriginalReturnType = originalReturnType,
                    OriginalText = $"{method.ReturnType} {method.Identifier}"
                };

                Plan.MethodSignatureChanges.Add(change);

                var annotatedMethod = method.WithAdditionalAnnotations(change.Annotation);
                currentRoot = currentRoot.ReplaceNode(method, annotatedMethod);
            }
            catch (Exception ex)
            {
                Plan.Failures.Add(new ConversionFailure
                {
                    Phase = "MethodSignatureAnalysis",
                    Description = ex.Message,
                    OriginalCode = method.Identifier.ToString(),
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    /// <summary>
    /// Analyzes the return type to determine what changes are needed for async conversion.
    /// </summary>
    /// <returns>
    /// A tuple of (changeReturnTypeToTask, wrapReturnTypeInTask, originalReturnType):
    /// - changeReturnTypeToTask: true if return type is void and should become Task
    /// - wrapReturnTypeInTask: true if return type is non-void, non-Task and should become Task&lt;T&gt;
    /// - originalReturnType: the original return type to wrap (only set when wrapReturnTypeInTask is true)
    /// </returns>
    private static (bool changeReturnTypeToTask, bool wrapReturnTypeInTask, string? originalReturnType) AnalyzeReturnTypeForAsync(string returnTypeText)
    {
        // void → Task
        if (returnTypeText == "void")
        {
            return (true, false, null);
        }

        // Already Task or Task<T> → no change needed
        if (returnTypeText == "Task" ||
            returnTypeText.StartsWith("Task<") ||
            returnTypeText.StartsWith("System.Threading.Tasks.Task"))
        {
            return (false, false, null);
        }

        // Already ValueTask or ValueTask<T> → no change needed (async already works with ValueTask)
        if (returnTypeText == "ValueTask" ||
            returnTypeText.StartsWith("ValueTask<") ||
            returnTypeText.StartsWith("System.Threading.Tasks.ValueTask"))
        {
            return (false, false, null);
        }

        // Non-void, non-Task return type → wrap in Task<T>
        // e.g., object → Task<object>, int → Task<int>
        return (false, true, returnTypeText);
    }

    /// <summary>
    /// Determines which usings to add and remove.
    /// </summary>
    protected abstract void AnalyzeUsings();

    /// <summary>
    /// Gets the original root for semantic model queries.
    /// Use this when you need to query the semantic model.
    /// </summary>
    protected CompilationUnitSyntax OriginalRoot => _originalRoot;
}
