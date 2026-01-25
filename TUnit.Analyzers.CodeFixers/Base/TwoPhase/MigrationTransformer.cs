using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Analyzers.Migrators.Base;

namespace TUnit.Analyzers.CodeFixers.Base.TwoPhase;

/// <summary>
/// Phase 2: Applies transformations based on the ConversionPlan.
/// Uses annotations to find nodes that need conversion.
/// No semantic model needed - pure syntax transformations.
/// </summary>
public class MigrationTransformer
{
    private readonly ConversionPlan _plan;
    private readonly string _frameworkName;

    public MigrationTransformer(ConversionPlan plan, string frameworkName)
    {
        _plan = plan;
        _frameworkName = frameworkName;
    }

    /// <summary>
    /// Applies all transformations from the conversion plan.
    /// </summary>
    public CompilationUnitSyntax Transform(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        // Apply transformations in dependency order:
        // 1. Record.Exception conversions (before assertions - may affect structure)
        currentRoot = TransformRecordExceptionCalls(currentRoot);

        // 2. Invocation replacements (ITestOutputHelper → Console)
        currentRoot = TransformInvocationReplacements(currentRoot);

        // 3. TheoryData conversions (TheoryData<T> → IEnumerable<T>)
        currentRoot = TransformTheoryData(currentRoot);

        // 4. Assertions (may introduce await)
        currentRoot = TransformAssertions(currentRoot);

        // 4. Method signatures (add async/Task based on new awaits)
        currentRoot = TransformMethodSignatures(currentRoot);

        // 5. Add method attributes (e.g., [Before(Test)])
        currentRoot = AddMethodAttributes(currentRoot);

        // 6. Attributes
        currentRoot = TransformAttributes(currentRoot);

        // 6b. Parameter attributes (e.g., [Range] → [MatrixRange])
        currentRoot = TransformParameterAttributes(currentRoot);

        // 7. Remove attributes
        currentRoot = RemoveAttributes(currentRoot);

        // 8. Remove base types
        currentRoot = RemoveBaseTypes(currentRoot);

        // 9. Add base types (e.g., IAsyncInitializer)
        currentRoot = AddBaseTypes(currentRoot);

        // 10. Add class attributes (e.g., ClassDataSource)
        currentRoot = AddClassAttributes(currentRoot);

        // 11. Remove members
        currentRoot = RemoveMembers(currentRoot);

        // 12. Remove constructor parameters
        currentRoot = RemoveConstructorParameters(currentRoot);

        // 13. Update usings (last, pure syntax)
        currentRoot = TransformUsings(currentRoot);

        // 14. Add TODO comments for failures
        if (_plan.HasFailures)
        {
            currentRoot = AddFailureComments(currentRoot);
        }

        return currentRoot;
    }

    private CompilationUnitSyntax TransformRecordExceptionCalls(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var conversion in _plan.RecordExceptionConversions)
        {
            try
            {
                // Find the annotated local declaration statement
                var statement = currentRoot.DescendantNodes()
                    .OfType<LocalDeclarationStatementSyntax>()
                    .FirstOrDefault(s => s.HasAnnotation(conversion.Annotation));

                if (statement == null) continue;

                // Get indentation from the statement
                var leadingTrivia = statement.GetLeadingTrivia();
                var indentation = leadingTrivia
                    .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                    .LastOrDefault()
                    .ToString();

                // Build the try body as a statement
                var tryBlockBody = conversion.TryBlockBody.Trim();
                // Ensure it ends with semicolon if not already
                if (!tryBlockBody.EndsWith(";") && !tryBlockBody.EndsWith("}"))
                {
                    tryBlockBody += ";";
                }

                var tryBodyStatement = SyntaxFactory.ParseStatement(tryBlockBody);
                var tryBlock = SyntaxFactory.Block(tryBodyStatement);

                // Build the catch block
                var catchAssignment = SyntaxFactory.ParseStatement($"{conversion.VariableName} = e;");
                var catchBlock = SyntaxFactory.Block(catchAssignment);

                var catchClause = SyntaxFactory.CatchClause()
                    .WithDeclaration(
                        SyntaxFactory.CatchDeclaration(
                            SyntaxFactory.IdentifierName("Exception"),
                            SyntaxFactory.Identifier("e")))
                    .WithBlock(catchBlock);

                var tryCatchStatement = SyntaxFactory.TryStatement()
                    .WithBlock(tryBlock)
                    .WithCatches(SyntaxFactory.SingletonList(catchClause));

                // Build the variable declaration with proper trailing newline
                var variableDecl = SyntaxFactory.ParseStatement($"Exception? {conversion.VariableName} = null;");

                // Create a list of statements to replace the original
                // Add newline after variable declaration and proper indentation for try statement
                var newStatements = new List<StatementSyntax>
                {
                    variableDecl
                        .WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n")),
                    tryCatchStatement
                        .WithLeadingTrivia(SyntaxFactory.Whitespace(indentation))
                        .WithTrailingTrivia(statement.GetTrailingTrivia())
                };

                // Find the containing block and replace the statement with the new statements
                var containingBlock = statement.Ancestors().OfType<BlockSyntax>().FirstOrDefault();
                if (containingBlock != null)
                {
                    var statementIndex = containingBlock.Statements.IndexOf(statement);
                    if (statementIndex >= 0)
                    {
                        var newStmtList = containingBlock.Statements
                            .RemoveAt(statementIndex)
                            .InsertRange(statementIndex, newStatements);
                        var newBlock = containingBlock.WithStatements(newStmtList);
                        currentRoot = currentRoot.ReplaceNode(containingBlock, newBlock);
                    }
                }
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "RecordExceptionTransformation",
                    Description = ex.Message,
                    OriginalCode = conversion.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax TransformInvocationReplacements(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var replacement in _plan.InvocationReplacements)
        {
            try
            {
                var invocation = currentRoot.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault(i => i.HasAnnotation(replacement.Annotation));

                if (invocation == null) continue;

                // Parse the replacement code
                var newInvocation = SyntaxFactory.ParseExpression(replacement.ReplacementCode);

                currentRoot = currentRoot.ReplaceNode(invocation, newInvocation
                    .WithLeadingTrivia(invocation.GetLeadingTrivia())
                    .WithTrailingTrivia(invocation.GetTrailingTrivia()));
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "InvocationReplacementTransformation",
                    Description = ex.Message,
                    OriginalCode = replacement.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax TransformTheoryData(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var conversion in _plan.TheoryDataConversions)
        {
            try
            {
                // First, transform the object creation expression to array creation
                if (conversion.CreationAnnotation != null)
                {
                    var objectCreation = currentRoot.DescendantNodes()
                        .OfType<BaseObjectCreationExpressionSyntax>()
                        .FirstOrDefault(n => n.HasAnnotation(conversion.CreationAnnotation));

                    if (objectCreation?.Initializer != null)
                    {
                        // Build array type: T[]
                        var arrayType = SyntaxFactory.ArrayType(
                            SyntaxFactory.ParseTypeName(conversion.ElementType),
                            SyntaxFactory.SingletonList(
                                SyntaxFactory.ArrayRankSpecifier(
                                    SyntaxFactory.SingletonSeparatedList<ExpressionSyntax>(
                                        SyntaxFactory.OmittedArraySizeExpression()
                                    )
                                )
                            )
                        ).WithoutTrailingTrivia();

                        // Get the open brace token and ensure it has proper newline trivia
                        var openBrace = objectCreation.Initializer.OpenBraceToken;
                        if (!openBrace.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
                        {
                            // Add newline and proper indentation before the brace
                            openBrace = openBrace.WithLeadingTrivia(
                                SyntaxFactory.EndOfLine("\n"),
                                SyntaxFactory.Whitespace("    "));
                        }

                        // Create array initializer from the collection initializer
                        var newInitializer = SyntaxFactory.InitializerExpression(
                            SyntaxKind.ArrayInitializerExpression,
                            openBrace,
                            objectCreation.Initializer.Expressions,
                            objectCreation.Initializer.CloseBraceToken);

                        // Build the array creation expression
                        var newKeyword = SyntaxFactory.Token(SyntaxKind.NewKeyword)
                            .WithLeadingTrivia(objectCreation.GetLeadingTrivia())
                            .WithTrailingTrivia(SyntaxFactory.Space);

                        var arrayCreation = SyntaxFactory.ArrayCreationExpression(
                            newKeyword,
                            arrayType,
                            newInitializer
                        ).WithTrailingTrivia(objectCreation.GetTrailingTrivia());

                        currentRoot = currentRoot.ReplaceNode(objectCreation, arrayCreation);
                    }
                }

                // Then, transform the type declaration from TheoryData<T> to IEnumerable<T>
                if (conversion.TypeAnnotation != null)
                {
                    var genericName = currentRoot.DescendantNodes()
                        .OfType<GenericNameSyntax>()
                        .FirstOrDefault(n => n.HasAnnotation(conversion.TypeAnnotation));

                    if (genericName != null)
                    {
                        var enumerableType = SyntaxFactory.GenericName(
                            SyntaxFactory.Identifier("IEnumerable"),
                            SyntaxFactory.TypeArgumentList(
                                SyntaxFactory.SeparatedList(genericName.TypeArgumentList.Arguments)))
                            .WithLeadingTrivia(genericName.GetLeadingTrivia())
                            .WithTrailingTrivia(genericName.GetTrailingTrivia());

                        currentRoot = currentRoot.ReplaceNode(genericName, enumerableType);
                    }
                }
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "TheoryDataTransformation",
                    Description = ex.Message,
                    OriginalCode = conversion.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax TransformAssertions(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var assertion in _plan.Assertions)
        {
            try
            {
                var node = currentRoot.DescendantNodes()
                    .OfType<InvocationExpressionSyntax>()
                    .FirstOrDefault(n => n.HasAnnotation(assertion.Annotation));

                if (node == null) continue;

                // Parse the replacement code
                var replacement = SyntaxFactory.ParseExpression(assertion.ReplacementCode);

                // Find the containing statement
                var containingStatement = node.Ancestors()
                    .OfType<ExpressionStatementSyntax>()
                    .FirstOrDefault();

                if (containingStatement != null)
                {
                    // Build the leading trivia, including TODO comment if present
                    var leadingTrivia = containingStatement.GetLeadingTrivia();
                    if (!string.IsNullOrEmpty(assertion.TodoComment))
                    {
                        // Extract the indentation from existing trivia
                        var indentationTrivia = leadingTrivia
                            .Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia))
                            .LastOrDefault();

                        var todoTrivia = new List<SyntaxTrivia>();
                        if (indentationTrivia != default)
                        {
                            todoTrivia.Add(indentationTrivia);
                        }
                        todoTrivia.Add(SyntaxFactory.Comment(assertion.TodoComment));
                        todoTrivia.Add(SyntaxFactory.EndOfLine("\n"));

                        // Combine TODO comment with existing leading trivia
                        leadingTrivia = SyntaxFactory.TriviaList(todoTrivia.Concat(leadingTrivia));
                    }

                    // Replace the entire statement with the new expression statement
                    var newStatement = SyntaxFactory.ExpressionStatement(replacement)
                        .WithLeadingTrivia(leadingTrivia)
                        .WithTrailingTrivia(containingStatement.GetTrailingTrivia());

                    currentRoot = currentRoot.ReplaceNode(containingStatement, newStatement);
                }
                else
                {
                    // Just replace the expression
                    currentRoot = currentRoot.ReplaceNode(node, replacement
                        .WithLeadingTrivia(node.GetLeadingTrivia())
                        .WithTrailingTrivia(node.GetTrailingTrivia()));
                }
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "AssertionTransformation",
                    Description = ex.Message,
                    OriginalCode = assertion.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax TransformMethodSignatures(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var change in _plan.MethodSignatureChanges)
        {
            try
            {
                var method = currentRoot.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.HasAnnotation(change.Annotation));

                if (method == null) continue;

                var newMethod = method;

                // Add async modifier if needed
                if (change.AddAsync && !method.Modifiers.Any(SyntaxKind.AsyncKeyword))
                {
                    var asyncToken = SyntaxFactory.Token(SyntaxKind.AsyncKeyword)
                        .WithTrailingTrivia(SyntaxFactory.Space);

                    // Insert async before the return type, after other modifiers
                    var newModifiers = method.Modifiers.Add(asyncToken);
                    newMethod = newMethod.WithModifiers(newModifiers);
                }

                // Change return type to Task if needed (from void)
                if (change.ChangeReturnTypeToTask && method.ReturnType.ToString() == "void")
                {
                    var taskType = SyntaxFactory.IdentifierName("Task")
                        .WithTrailingTrivia(SyntaxFactory.Space);
                    newMethod = newMethod.WithReturnType(taskType);
                }

                // Wrap return type in Task<T> if needed (non-void, non-Task return type)
                if (change.WrapReturnTypeInTask && !string.IsNullOrEmpty(change.OriginalReturnType))
                {
                    // Build Task<OriginalReturnType>
                    var taskGenericType = SyntaxFactory.GenericName(
                        SyntaxFactory.Identifier("Task"),
                        SyntaxFactory.TypeArgumentList(
                            SyntaxFactory.SingletonSeparatedList(
                                SyntaxFactory.ParseTypeName(change.OriginalReturnType))))
                        .WithTrailingTrivia(SyntaxFactory.Space);
                    newMethod = newMethod.WithReturnType(taskGenericType);
                }

                // Change ValueTask to Task if needed (for IAsyncLifetime.InitializeAsync → IAsyncInitializer)
                if (change.ChangeValueTaskToTask && method.ReturnType.ToString() == "ValueTask")
                {
                    var taskType = SyntaxFactory.IdentifierName("Task")
                        .WithTrailingTrivia(SyntaxFactory.Space);
                    newMethod = newMethod.WithReturnType(taskType);
                }

                // Make method public if needed (for lifecycle methods)
                if (change.MakePublic)
                {
                    var hasPublicModifier = newMethod.Modifiers.Any(SyntaxKind.PublicKeyword);
                    if (!hasPublicModifier)
                    {
                        // Remove existing access modifiers (private, protected, internal)
                        var newModifiers = new SyntaxTokenList();
                        var publicToken = SyntaxFactory.Token(SyntaxKind.PublicKeyword)
                            .WithTrailingTrivia(SyntaxFactory.Space);

                        // Add public at the start
                        newModifiers = newModifiers.Add(publicToken);

                        // Keep non-access modifiers (static, async, etc.)
                        foreach (var modifier in newMethod.Modifiers)
                        {
                            if (!modifier.IsKind(SyntaxKind.PrivateKeyword) &&
                                !modifier.IsKind(SyntaxKind.ProtectedKeyword) &&
                                !modifier.IsKind(SyntaxKind.InternalKeyword) &&
                                !modifier.IsKind(SyntaxKind.PublicKeyword))
                            {
                                newModifiers = newModifiers.Add(modifier);
                            }
                        }

                        newMethod = newMethod.WithModifiers(newModifiers);
                    }
                }

                if (newMethod != method)
                {
                    currentRoot = currentRoot.ReplaceNode(method, newMethod);
                }
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "MethodSignatureTransformation",
                    Description = ex.Message,
                    OriginalCode = change.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax TransformAttributes(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var conversion in _plan.Attributes)
        {
            try
            {
                var attribute = currentRoot.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .FirstOrDefault(a => a.HasAnnotation(conversion.Annotation));

                if (attribute == null) continue;

                // Build new attribute - handle generic names like ClassDataSource<MyType>
                AttributeSyntax newAttribute;
                if (conversion.NewAttributeName.Contains('<'))
                {
                    // Generic attribute name - parse it properly
                    var fullAttrCode = conversion.NewArgumentList != null && conversion.NewArgumentList.Length > 0
                        ? conversion.NewAttributeName + conversion.NewArgumentList
                        : conversion.NewArgumentList == null && attribute.ArgumentList != null
                            ? conversion.NewAttributeName + attribute.ArgumentList
                            : conversion.NewAttributeName;

                    newAttribute = ParseAttributeCode(fullAttrCode);
                }
                else
                {
                    // Use ParseName for qualified names (e.g., System.Obsolete)
                    var newName = conversion.NewAttributeName.Contains('.')
                        ? SyntaxFactory.ParseName(conversion.NewAttributeName)
                        : (NameSyntax)SyntaxFactory.IdentifierName(conversion.NewAttributeName);
                    newAttribute = SyntaxFactory.Attribute(newName);

                    // Add argument list if specified
                    if (conversion.NewArgumentList != null && conversion.NewArgumentList.Length > 0)
                    {
                        var argList = SyntaxFactory.ParseAttributeArgumentList(conversion.NewArgumentList);
                        newAttribute = newAttribute.WithArgumentList(argList);
                    }
                    else if (conversion.NewArgumentList == null && attribute.ArgumentList != null)
                    {
                        // Keep original arguments
                        newAttribute = newAttribute.WithArgumentList(attribute.ArgumentList);
                    }
                }

                // Handle additional attributes (e.g., Skip from Fact(Skip = "reason"))
                if (conversion.AdditionalAttributes != null && conversion.AdditionalAttributes.Count > 0)
                {
                    // Find the containing attribute list
                    var attributeList = attribute.Ancestors()
                        .OfType<AttributeListSyntax>()
                        .FirstOrDefault();

                    if (attributeList != null)
                    {
                        // Create separate attribute lists - each additional attribute on its own line
                        var newAttributeLists = new List<AttributeListSyntax>();

                        // Extract just the indentation from leading trivia (whitespace at the end)
                        var fullLeadingTrivia = attributeList.GetLeadingTrivia();
                        var indentationTrivia = SyntaxFactory.TriviaList(
                            fullLeadingTrivia.Where(t => t.IsKind(SyntaxKind.WhitespaceTrivia)));

                        // First, create the attribute list with the main converted attribute
                        // Keep the full leading trivia (including any blank lines before) for the first attribute
                        var mainAttrList = SyntaxFactory.AttributeList(
                                SyntaxFactory.SingletonSeparatedList(newAttribute))
                            .WithLeadingTrivia(fullLeadingTrivia)
                            .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));
                        newAttributeLists.Add(mainAttrList);

                        // Create separate attribute lists for each additional attribute
                        foreach (var additional in conversion.AdditionalAttributes)
                        {
                            var additionalAttr = SyntaxFactory.Attribute(
                                SyntaxFactory.IdentifierName(additional.Name));

                            if (!string.IsNullOrEmpty(additional.Arguments))
                            {
                                additionalAttr = additionalAttr.WithArgumentList(
                                    SyntaxFactory.ParseAttributeArgumentList(additional.Arguments));
                            }

                            // Use only indentation for additional attributes (no blank lines)
                            var additionalAttrList = SyntaxFactory.AttributeList(
                                    SyntaxFactory.SingletonSeparatedList(additionalAttr))
                                .WithLeadingTrivia(indentationTrivia)
                                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));
                            newAttributeLists.Add(additionalAttrList);
                        }

                        // Replace original attribute list with multiple new ones
                        currentRoot = currentRoot.ReplaceNode(attributeList, newAttributeLists);
                        continue;
                    }
                }

                currentRoot = currentRoot.ReplaceNode(attribute, newAttribute
                    .WithLeadingTrivia(attribute.GetLeadingTrivia())
                    .WithTrailingTrivia(attribute.GetTrailingTrivia()));
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "AttributeTransformation",
                    Description = ex.Message,
                    OriginalCode = conversion.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax TransformParameterAttributes(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var conversion in _plan.ParameterAttributes)
        {
            try
            {
                var attribute = currentRoot.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .FirstOrDefault(a => a.HasAnnotation(conversion.Annotation));

                if (attribute == null) continue;

                // Build new attribute - handle generic names like MatrixRange<int>
                AttributeSyntax newAttribute;
                if (conversion.NewAttributeName.Contains('<'))
                {
                    // Generic attribute name - parse it properly
                    var fullAttrCode = conversion.NewArgumentList != null && conversion.NewArgumentList.Length > 0
                        ? conversion.NewAttributeName + conversion.NewArgumentList
                        : conversion.NewArgumentList == null && attribute.ArgumentList != null
                            ? conversion.NewAttributeName + attribute.ArgumentList
                            : conversion.NewAttributeName;

                    newAttribute = ParseAttributeCode(fullAttrCode);
                }
                else
                {
                    var newName = (NameSyntax)SyntaxFactory.IdentifierName(conversion.NewAttributeName);
                    newAttribute = SyntaxFactory.Attribute(newName);

                    // Add argument list if specified
                    if (conversion.NewArgumentList != null && conversion.NewArgumentList.Length > 0)
                    {
                        var argList = SyntaxFactory.ParseAttributeArgumentList(conversion.NewArgumentList);
                        newAttribute = newAttribute.WithArgumentList(argList);
                    }
                    else if (conversion.NewArgumentList == null && attribute.ArgumentList != null)
                    {
                        // Keep original arguments
                        newAttribute = newAttribute.WithArgumentList(attribute.ArgumentList);
                    }
                }

                // Preserve trivia
                newAttribute = newAttribute
                    .WithLeadingTrivia(attribute.GetLeadingTrivia())
                    .WithTrailingTrivia(attribute.GetTrailingTrivia());

                currentRoot = currentRoot.ReplaceNode(attribute, newAttribute);
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "ParameterAttributeTransformation",
                    Description = ex.Message,
                    OriginalCode = conversion.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax RemoveAttributes(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var removal in _plan.AttributeRemovals)
        {
            try
            {
                var attribute = currentRoot.DescendantNodes()
                    .OfType<AttributeSyntax>()
                    .FirstOrDefault(a => a.HasAnnotation(removal.Annotation));

                if (attribute == null) continue;

                // Find the attribute list containing this attribute
                var attributeList = attribute.Ancestors()
                    .OfType<AttributeListSyntax>()
                    .FirstOrDefault();

                if (attributeList == null) continue;

                if (attributeList.Attributes.Count == 1)
                {
                    // Remove the entire attribute list without keeping its trivia
                    // This prevents extra indentation from being left behind
                    currentRoot = currentRoot.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia)!;
                }
                else
                {
                    // Remove just this attribute from the list
                    var newAttributes = attributeList.Attributes.Remove(attribute);
                    var newList = attributeList.WithAttributes(newAttributes);
                    currentRoot = currentRoot.ReplaceNode(attributeList, newList);
                }
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "AttributeRemoval",
                    Description = ex.Message,
                    OriginalCode = removal.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax RemoveBaseTypes(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var removal in _plan.BaseTypeRemovals)
        {
            try
            {
                var baseType = currentRoot.DescendantNodes()
                    .OfType<BaseTypeSyntax>()
                    .FirstOrDefault(b => b.HasAnnotation(removal.Annotation));

                if (baseType == null) continue;

                var baseList = baseType.Ancestors()
                    .OfType<BaseListSyntax>()
                    .FirstOrDefault();

                if (baseList == null) continue;

                if (baseList.Types.Count == 1)
                {
                    // Remove the entire base list
                    var classDecl = baseList.Ancestors()
                        .OfType<ClassDeclarationSyntax>()
                        .FirstOrDefault();

                    if (classDecl != null)
                    {
                        var newClass = classDecl.WithBaseList(null);

                        // Remove trailing trivia from the element before the base list
                        // This could be ParameterList, TypeParameterList, or Identifier
                        if (classDecl.ParameterList != null)
                        {
                            // Primary constructor - remove trailing trivia from the close paren
                            var paramList = classDecl.ParameterList;
                            var closeParen = paramList.CloseParenToken.WithTrailingTrivia(SyntaxFactory.TriviaList());
                            newClass = newClass.WithParameterList(paramList.WithCloseParenToken(closeParen));
                        }
                        else if (classDecl.TypeParameterList != null)
                        {
                            // Generic class - remove trailing trivia from the close angle bracket
                            var typeParamList = classDecl.TypeParameterList;
                            var closeAngle = typeParamList.GreaterThanToken.WithTrailingTrivia(SyntaxFactory.TriviaList());
                            newClass = newClass.WithTypeParameterList(typeParamList.WithGreaterThanToken(closeAngle));
                        }
                        else
                        {
                            // Regular class - the identifier might have trailing trivia
                            newClass = newClass.WithIdentifier(classDecl.Identifier.WithTrailingTrivia(SyntaxFactory.TriviaList()));
                        }

                        // Preserve the open brace trivia - it should have newline before it
                        if (classDecl.OpenBraceToken != default)
                        {
                            var openBrace = classDecl.OpenBraceToken;
                            if (!openBrace.LeadingTrivia.Any(t => t.IsKind(SyntaxKind.EndOfLineTrivia)))
                            {
                                // Add newline before the open brace
                                openBrace = openBrace.WithLeadingTrivia(SyntaxFactory.EndOfLine("\n"));
                            }
                            newClass = newClass.WithOpenBraceToken(openBrace);
                        }

                        currentRoot = currentRoot.ReplaceNode(classDecl, newClass);
                    }
                }
                else
                {
                    // Remove just this base type
                    var newTypes = baseList.Types.Remove(baseType);
                    var newList = baseList.WithTypes(newTypes);
                    currentRoot = currentRoot.ReplaceNode(baseList, newList);
                }
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "BaseTypeRemoval",
                    Description = ex.Message,
                    OriginalCode = removal.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax AddBaseTypes(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var addition in _plan.BaseTypeAdditions)
        {
            try
            {
                // Find the class declaration that has the annotation
                var classDecl = currentRoot.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.HasAnnotation(addition.Annotation));

                if (classDecl == null) continue;

                // Create the new base type
                var newBaseType = SyntaxFactory.SimpleBaseType(
                    SyntaxFactory.ParseTypeName(addition.TypeName));

                ClassDeclarationSyntax newClass;
                if (classDecl.BaseList == null)
                {
                    // Create new base list
                    var baseList = SyntaxFactory.BaseList(
                        SyntaxFactory.SingletonSeparatedList<BaseTypeSyntax>(newBaseType))
                        .WithColonToken(SyntaxFactory.Token(SyntaxKind.ColonToken).WithTrailingTrivia(SyntaxFactory.Space));
                    newClass = classDecl.WithBaseList(baseList);
                }
                else
                {
                    // Add to existing base list
                    var newTypes = classDecl.BaseList.Types.Add(newBaseType);
                    var newBaseList = classDecl.BaseList.WithTypes(newTypes);
                    newClass = classDecl.WithBaseList(newBaseList);
                }

                currentRoot = currentRoot.ReplaceNode(classDecl, newClass);
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "BaseTypeAddition",
                    Description = ex.Message,
                    OriginalCode = addition.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax AddClassAttributes(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var addition in _plan.ClassAttributeAdditions)
        {
            try
            {
                // Find the class declaration that has the annotation
                var classDecl = currentRoot.DescendantNodes()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault(c => c.HasAnnotation(addition.Annotation));

                if (classDecl == null) continue;

                // Parse the complete attribute (handles generic types and arguments)
                // Format: "ClassDataSource<MyType>(Shared = SharedType.PerClass)"
                var attrCode = addition.AttributeCode;
                var attribute = ParseAttributeCode(attrCode);

                // Preserve class's leading trivia and put attribute list before it
                var classLeadingTrivia = classDecl.GetLeadingTrivia();
                var attributeList = SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(attribute))
                    .WithLeadingTrivia(classLeadingTrivia)
                    .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

                // Remove leading trivia from class (it's now on the attribute)
                var newClass = classDecl
                    .WithLeadingTrivia(SyntaxFactory.TriviaList())
                    .AddAttributeLists(attributeList);

                currentRoot = currentRoot.ReplaceNode(classDecl, newClass);
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "ClassAttributeAddition",
                    Description = ex.Message,
                    OriginalCode = addition.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    /// <summary>
    /// Parses an attribute code string that may include generic type arguments and parameters.
    /// Format: "AttributeName" or "GenericAttr<T>" or "Attr(args)" or "GenericAttr<T>(args)"
    /// </summary>
    private static AttributeSyntax ParseAttributeCode(string attrCode)
    {
        // Parse as a complete attribute by wrapping in a dummy class
        var code = $"[{attrCode}] class Dummy {{ }}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var attr = tree.GetRoot()
            .DescendantNodes()
            .OfType<AttributeSyntax>()
            .FirstOrDefault();

        return attr ?? SyntaxFactory.Attribute(SyntaxFactory.IdentifierName(attrCode));
    }

    private CompilationUnitSyntax AddMethodAttributes(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var addition in _plan.MethodAttributeAdditions)
        {
            try
            {
                // Find the method declaration that has the annotation
                var method = currentRoot.DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.HasAnnotation(addition.Annotation));

                if (method == null) continue;

                // Parse and create the new attribute
                // AttributeCode may include arguments like "Before(Test)" or just a name like "Test"
                AttributeSyntax attribute;
                var parenIndex = addition.AttributeCode.IndexOf('(');
                if (parenIndex > 0)
                {
                    // Has arguments: split into name and argument list
                    var attrName = addition.AttributeCode.Substring(0, parenIndex);
                    var argList = addition.AttributeCode.Substring(parenIndex);
                    attribute = SyntaxFactory.Attribute(
                        SyntaxFactory.ParseName(attrName),
                        SyntaxFactory.ParseAttributeArgumentList(argList));
                }
                else
                {
                    // No arguments: just the name
                    attribute = SyntaxFactory.Attribute(SyntaxFactory.ParseName(addition.AttributeCode));
                }
                // Get the leading trivia from the first attribute (if any) or method
                var leadingTrivia = method.AttributeLists.Count > 0
                    ? method.AttributeLists[0].GetLeadingTrivia()
                    : method.GetLeadingTrivia();

                // The leading trivia typically contains: [newlines...] [whitespace for indent]
                // We want to:
                // 1. Put the full leading trivia (including blank lines) on the new [Test] attribute
                // 2. Put ONLY the trailing whitespace (indentation) on the first existing attribute
                var triviaList = leadingTrivia.ToList();

                // Find the last whitespace trivia - that's the indentation
                var lastWhitespaceIndex = -1;
                for (int i = triviaList.Count - 1; i >= 0; i--)
                {
                    if (triviaList[i].IsKind(SyntaxKind.WhitespaceTrivia))
                    {
                        lastWhitespaceIndex = i;
                        break;
                    }
                }

                // Indentation is just the final whitespace trivia (if any)
                var indentationTrivia = lastWhitespaceIndex >= 0
                    ? SyntaxFactory.TriviaList(triviaList[lastWhitespaceIndex])
                    : SyntaxFactory.TriviaList();

                // Build the new list of attribute lists manually
                var newAttributeLists = new List<AttributeListSyntax>();

                // Add the new [Test] attribute first with full leading trivia
                // Explicitly clear trailing trivia - the first existing attribute's leading trivia has the newline
                var newTestAttrList = SyntaxFactory.AttributeList(
                    SyntaxFactory.SingletonSeparatedList(attribute))
                    .WithLeadingTrivia(leadingTrivia)
                    .WithTrailingTrivia(SyntaxFactory.TriviaList());
                newAttributeLists.Add(newTestAttrList);

                // Add existing attributes, updating the first one's leading trivia
                for (int i = 0; i < method.AttributeLists.Count; i++)
                {
                    var existingAttr = method.AttributeLists[i];
                    if (i == 0)
                    {
                        // First existing attribute: need newline + indentation
                        // The newline separates it from [Test], the indentation aligns it
                        var newLeading = SyntaxFactory.TriviaList(
                            SyntaxFactory.EndOfLine("\n"))
                            .AddRange(indentationTrivia);
                        existingAttr = existingAttr.WithLeadingTrivia(newLeading);
                    }
                    newAttributeLists.Add(existingAttr);
                }

                var newMethod = method
                    .WithLeadingTrivia(SyntaxFactory.TriviaList())
                    .WithAttributeLists(SyntaxFactory.List(newAttributeLists));

                // Change return type if specified
                if (!string.IsNullOrEmpty(addition.NewReturnType))
                {
                    newMethod = newMethod.WithReturnType(
                        SyntaxFactory.ParseTypeName(addition.NewReturnType)
                            .WithTrailingTrivia(SyntaxFactory.Space));
                }

                currentRoot = currentRoot.ReplaceNode(method, newMethod);
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "MethodAttributeAddition",
                    Description = ex.Message,
                    OriginalCode = addition.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax RemoveMembers(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var removal in _plan.MemberRemovals)
        {
            try
            {
                var member = currentRoot.DescendantNodes()
                    .OfType<MemberDeclarationSyntax>()
                    .FirstOrDefault(m => m.HasAnnotation(removal.Annotation));

                if (member == null) continue;

                // Use KeepTrailingTrivia to preserve the newline after the member,
                // but NOT KeepLeadingTrivia which would transfer the member's indentation to the next node
                currentRoot = currentRoot.RemoveNode(member, SyntaxRemoveOptions.KeepTrailingTrivia)!;
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "MemberRemoval",
                    Description = ex.Message,
                    OriginalCode = removal.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax RemoveConstructorParameters(CompilationUnitSyntax root)
    {
        var currentRoot = root;

        foreach (var removal in _plan.ConstructorParameterRemovals)
        {
            try
            {
                var parameter = currentRoot.DescendantNodes()
                    .OfType<ParameterSyntax>()
                    .FirstOrDefault(p => p.HasAnnotation(removal.Annotation));

                if (parameter == null) continue;

                var parameterList = parameter.Ancestors()
                    .OfType<ParameterListSyntax>()
                    .FirstOrDefault();

                if (parameterList == null) continue;

                var newParams = parameterList.Parameters.Remove(parameter);
                var newList = parameterList.WithParameters(newParams);
                currentRoot = currentRoot.ReplaceNode(parameterList, newList);
            }
            catch (Exception ex)
            {
                _plan.Failures.Add(new ConversionFailure
                {
                    Phase = "ConstructorParameterRemoval",
                    Description = ex.Message,
                    OriginalCode = removal.OriginalText,
                    Exception = ex
                });
            }
        }

        return currentRoot;
    }

    private CompilationUnitSyntax TransformUsings(CompilationUnitSyntax root)
    {
        // Remove framework usings
        root = MigrationHelpers.RemoveFrameworkUsings(root, _frameworkName);

        // Add TUnit usings (handled by MigrationHelpers which checks for async code, File/Directory usage, etc.)
        root = MigrationHelpers.AddTUnitUsings(root);

        return root;
    }

    private CompilationUnitSyntax AddFailureComments(CompilationUnitSyntax root)
    {
        var failureSummary = _plan.Failures
            .GroupBy(f => f.Phase)
            .Select(g => $"// TODO: TUnit migration - {g.Key}: {g.Count()} item(s) could not be converted")
            .ToList();

        if (failureSummary.Count == 0)
        {
            return root;
        }

        var commentTrivia = new List<SyntaxTrivia>
        {
            SyntaxFactory.Comment("// ============================================================"),
            SyntaxFactory.EndOfLine("\n"),
            SyntaxFactory.Comment("// TUnit Migration: Some items require manual attention"),
            SyntaxFactory.EndOfLine("\n")
        };

        foreach (var summary in failureSummary)
        {
            commentTrivia.Add(SyntaxFactory.Comment(summary));
            commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));
        }

        commentTrivia.Add(SyntaxFactory.Comment("// ============================================================"));
        commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));
        commentTrivia.Add(SyntaxFactory.EndOfLine("\n"));

        var existingTrivia = root.GetLeadingTrivia();
        return root.WithLeadingTrivia(SyntaxFactory.TriviaList(commentTrivia).AddRange(existingTrivia));
    }
}
