﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis.LanguageService;
using Microsoft.CodeAnalysis.Shared.Extensions;

namespace Microsoft.CodeAnalysis.UseCollectionInitializer;

internal static class UpdateObjectCreationHelpers
{
    public static UpdateExpressionState<TExpressionSyntax, TStatementSyntax>? TryInitializeVariableDeclarationCase<TExpressionSyntax, TStatementSyntax>(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TExpressionSyntax rootExpression,
        TStatementSyntax containingStatement,
        CancellationToken cancellationToken)
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
    {
        if (!syntaxFacts.IsLocalDeclarationStatement(containingStatement))
            return null;

        var containingDeclarator = rootExpression.Parent?.Parent;
        if (containingDeclarator is null)
            return null;

        var initializedSymbol = semanticModel.GetDeclaredSymbol(containingDeclarator, cancellationToken);
        if (initializedSymbol is ILocalSymbol local &&
            local.Type is IDynamicTypeSymbol)
        {
            // Not supported if we're creating a dynamic local.  The object we're instantiating
            // may not have the members that we're trying to access on the dynamic object.
            return null;
        }

        if (!syntaxFacts.IsDeclaratorOfLocalDeclarationStatement(containingDeclarator, containingStatement))
            return null;

        var valuePattern = syntaxFacts.GetIdentifierOfVariableDeclarator(containingDeclarator);
        return new(semanticModel, syntaxFacts, rootExpression, valuePattern, initializedSymbol);
    }

    public static UpdateExpressionState<TExpressionSyntax, TStatementSyntax>? TryInitializeAssignmentCase<TExpressionSyntax, TStatementSyntax>(
        SemanticModel semanticModel,
        ISyntaxFacts syntaxFacts,
        TExpressionSyntax rootExpression,
        TStatementSyntax containingStatement,
        CancellationToken cancellationToken)
        where TExpressionSyntax : SyntaxNode
        where TStatementSyntax : SyntaxNode
    {
        if (!syntaxFacts.IsSimpleAssignmentStatement(containingStatement))
            return null;

        syntaxFacts.GetPartsOfAssignmentStatement(containingStatement,
            out var left, out var right);
        if (right != rootExpression)
            return null;

        var typeInfo = semanticModel.GetTypeInfo(left, cancellationToken);
        if (typeInfo.Type is IDynamicTypeSymbol || typeInfo.ConvertedType is IDynamicTypeSymbol)
        {
            // Not supported if we're initializing something dynamic.  The object we're instantiating
            // may not have the members that we're trying to access on the dynamic object.
            return null;
        }

        var initializedSymbol = semanticModel.GetSymbolInfo(left, cancellationToken).GetAnySymbol();
        return new(semanticModel, syntaxFacts, rootExpression, left, initializedSymbol);
    }
}
