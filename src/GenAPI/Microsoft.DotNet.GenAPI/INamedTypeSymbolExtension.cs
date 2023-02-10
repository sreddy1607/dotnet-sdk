﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.DotNet.ApiSymbolExtensions.Filtering;

namespace Microsoft.DotNet.GenAPI
{
    internal static class INamedTypeSymbolExtension
    {
        public static bool HasIndexer(this INamedTypeSymbol type)
        {
            return type.GetMembers().Any(member => member is IPropertySymbol propertySymbol && propertySymbol.IsIndexer);
        }

        // Visit a type and all its members, checking for cycles. Return true if the visitor returns true.
        private static bool WalkTypeSymbol(ITypeSymbol ty, HashSet<ITypeSymbol> visited, Func<ITypeSymbol, bool> f)
        {
            visited.Add(ty);

            if (f(ty))
            {
                return true;
            }

            foreach(INamedTypeSymbol memberType in ty.GetTypeMembers())
            {
                if (!visited.Contains(memberType) && WalkTypeSymbol(memberType, visited, f))
                {
                    return true;
                }
            }
            
            return false;
        }

        // Walk type with predicate that checks if a type is a reference type or ref-like (e.g. ByReference<T>).
        private static bool IsOrContainsReferenceType(ITypeSymbol ty) =>
            WalkTypeSymbol(ty, new(SymbolEqualityComparer.Default), ty => ty.IsRefLikeType || ty.IsReferenceType);

        // Walk type with predicate that checks if a type is unmanaged or a reference that's not the root.
        private static bool IsOrContainsNonEmptyStruct(ITypeSymbol root) =>
            WalkTypeSymbol(root, new(SymbolEqualityComparer.Default), ty => 
                ty.IsUnmanagedType || 
                    ((ty.IsReferenceType || ty.IsRefLikeType) && !SymbolEqualityComparer.Default.Equals(root, ty)));

        // Convert IEnumerable<AttributeData> to a SyntaxList<AttributeListSyntax>.
        private static SyntaxList<AttributeListSyntax> FromAttributeData(IEnumerable<AttributeData> attrData)
        {
            IEnumerable<SyntaxNode?> syntaxNodes = attrData.Select(ad =>
                ad.ApplicationSyntaxReference?.GetSyntax(new System.Threading.CancellationToken(false)));
            
            IEnumerable<AttributeListSyntax?> asNodes = syntaxNodes.Select(sn =>
            {
                if (sn is AttributeSyntax atSyntax) {
                    SeparatedSyntaxList<AttributeSyntax> singletonList = SyntaxFactory.SingletonSeparatedList<AttributeSyntax>(atSyntax);
                    AttributeListSyntax alSyntax = SyntaxFactory.AttributeList(singletonList);
                    return alSyntax;
                }

                return null;
            });
            
            List<AttributeListSyntax> asList = asNodes.Where(a => a != null).OfType<AttributeListSyntax>().ToList();
            return SyntaxFactory.List(asList);
        }

        // Build dummy field from a type, field name, and attribute list.
        private static SyntaxNode CreateDummyField(string typ, string fieldName, SyntaxList<AttributeListSyntax> attrs) =>
            SyntaxFactory.FieldDeclaration(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName(typ))
                .WithVariables(
                    SyntaxFactory.SingletonSeparatedList<Microsoft.CodeAnalysis.CSharp.Syntax.VariableDeclaratorSyntax>(
                        SyntaxFactory.VariableDeclarator(
                            SyntaxFactory.Identifier(fieldName))))
            ).WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword) })
            ).WithAttributeLists(attrs);

        // SynthesizeDummyFields yields private fields for the namedType, because they can be part of the API contract.
        // - A struct containing a field that is a reference type cannot be used as a reference.
        // - A struct containing nonempty fields needs to be fully initialized. (See "definite assignment" rules)
        //   - "non-empty" means either unmanaged types like ints and enums, or reference types that are not the root.
        // - A struct containing generic fields cannot have struct layout cycles.
        public static IEnumerable<SyntaxNode> SynthesizeDummyFields(this INamedTypeSymbol namedType, ISymbolFilter symbolFilter)
        {
            // Collect all excluded fields
            IEnumerable<IFieldSymbol> excludedFields = namedType.GetMembers()
                .Where(member => !symbolFilter.Include(member) && member is IFieldSymbol)
                .Select(m => (IFieldSymbol)m);
            
            if (excludedFields.Any())
            {
                // Collect generic excluded fields
                IEnumerable<IFieldSymbol> genericTypedFields = excludedFields.Where(f => {
                    if (f.Type is INamedTypeSymbol ty) {
                        return ty.IsGenericType;
                    }
                    return f.Type is ITypeParameterSymbol;
                });

                // Add a dummy field for each generic excluded field
                // // TODO: add this back when we properly handle generic fields.
                // foreach(IFieldSymbol genericField in genericTypedFields)
                // {
                //     yield return CreateDummyField(
                //         genericField.Type.ToDisplayString(),
                //         genericField.Name,
                //         FromAttributeData(genericField.GetAttributes()));
                // }

                // If any field's type is transitively a reference type.
                if (excludedFields.Any(f => IsOrContainsReferenceType(f.Type)))
                {
                    // add reference type dummy field
                    yield return CreateDummyField("object", "_dummy", new());

                    // add int field
                    yield return CreateDummyField("int", "_dummyPrimitive", new());
                }
                // Otherwise, if the field transitively contains a field whose type is non-empty.
                else if (excludedFields.Any(f => IsOrContainsNonEmptyStruct(f.Type)))
                {
                    // add int field
                    yield return CreateDummyField("int", "_dummyPrimitive", new());
                }
            }
        }
    }
}
