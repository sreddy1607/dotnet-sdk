﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.DotNet.ApiSymbolExtensions
{
    public static class SymbolExtensions
    {
        private static readonly SymbolDisplayFormat s_comparisonFormat;
        public static readonly SymbolDisplayFormat DisplayFormat;

        static SymbolExtensions()
        {
            // This is the default format for symbol.ToDisplayString;
            SymbolDisplayFormat format = SymbolDisplayFormat.CSharpErrorMessageFormat;
            format = format.AddMemberOptions(SymbolDisplayMemberOptions.IncludeType);
            format = format.AddParameterOptions(SymbolDisplayParameterOptions.IncludeExtensionThis);

            DisplayFormat = format;

            // Remove ? annotations from reference types as we want to map the APIs without nullable annotations
            // and have a special rule to catch those differences.
            // Also don't use keyword names for special types. This makes the comparison more accurate when no
            // references are running or if one side has references and the other doesn't.
            format = format.RemoveMiscellaneousOptions(
                SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
                SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName |
                SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

            // Remove ref/out from parameters to compare APIs when building the mappers.
            s_comparisonFormat = format.RemoveParameterOptions(SymbolDisplayParameterOptions.IncludeParamsRefOut);
        }

        public static string ToComparisonDisplayString(this ISymbol symbol)
        {
            var parts = symbol.ToDisplayParts(s_comparisonFormat);

            // Type parameter names are not significant. Remove these leaving behind
            // Angle brackets and commas for each parameter > 1.
            parts = parts.RemoveAll(p => p.Kind == SymbolDisplayPartKind.TypeParameterName);

            return parts
                  .ToDisplayString()
                  .Replace("System.IntPtr", "nint") // Treat IntPtr and nint as the same
                  .Replace("System.UIntPtr", "nuint"); // Treat UIntPtr and nuint as the same
        }

        public static IEnumerable<ITypeSymbol> GetAllBaseTypes(this ITypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Interface)
            {
                foreach (ITypeSymbol @interface in type.Interfaces)
                {
                    yield return @interface;
                    foreach (ITypeSymbol baseInterface in @interface.GetAllBaseTypes())
                        yield return baseInterface;
                }
            }
            else if (type.BaseType != null)
            {
                yield return type.BaseType;
                foreach (ITypeSymbol baseType in type.BaseType.GetAllBaseTypes())
                    yield return baseType;
            }
        }

        /// <summary>
        /// Determines if a symbol was generated by the compiler by checking for the presense of the CompilerGeneratedAttribute.
        /// </summary>
        /// <param name="symbol">Symbol to check</param>
        /// <returns>True if the attribute was found.</returns>
        public static bool IsCompilerGenerated(this ISymbol symbol)
        {
            return symbol.GetAttributes().Any(attribute =>
                attribute?.AttributeClass?.Name == "CompilerGeneratedAttribute" &&
                attribute.AttributeClass.ContainingNamespace.ToDisplayString().Equals("System.Runtime.CompilerServices", StringComparison.Ordinal));
        }

        public static bool IsEffectivelySealed(this ITypeSymbol type, bool includeInternalSymbols) =>
            type.IsSealed || !HasVisibleConstructor(type, includeInternalSymbols);

        /// <summary>
        /// Determines where the symbol is the explicit interface implementation method or property.
        /// </summary>
        /// <param name="symbol"><see cref="ISymbol"/>  Represents a symbol (namespace, class, method, parameter, etc.) exposed by the compiler.</param>
        /// <returns>true if the symbol is the explicit interface implementation method</returns>
        public static bool IsExplicitInterfaceImplementation(this ISymbol symbol) =>
            symbol is IMethodSymbol method && method.MethodKind == MethodKind.ExplicitInterfaceImplementation ||
            symbol is IPropertySymbol property && !property.ExplicitInterfaceImplementations.IsEmpty;

        private static bool HasVisibleConstructor(ITypeSymbol type, bool includeInternalSymbols)
        {
            if (type is INamedTypeSymbol namedType)
            {
                foreach (IMethodSymbol constructor in namedType.Constructors)
                {
                    if (!constructor.IsStatic && constructor.IsVisibleOutsideOfAssembly(includeInternalSymbols, includeEffectivelyPrivateSymbols: true))
                        return true;
                }
            }

            return false;
        }

        public static IEnumerable<ITypeSymbol> GetAllBaseInterfaces(this ITypeSymbol type)
        {
            foreach (ITypeSymbol @interface in type.Interfaces)
            {
                yield return @interface;
                foreach (ITypeSymbol baseInterface in @interface.GetAllBaseInterfaces())
                    yield return baseInterface;
            }

            foreach (ITypeSymbol baseType in type.GetAllBaseTypes())
                foreach (ITypeSymbol baseInterface in baseType.GetAllBaseInterfaces())
                    yield return baseInterface;
        }

        public static bool IsVisibleOutsideOfAssembly(this ISymbol symbol,
            bool includeInternalSymbols,
            bool includeEffectivelyPrivateSymbols = false,
            bool includeExplicitInterfaceImplementationSymbols = false) =>
            symbol.DeclaredAccessibility switch
            {
                Accessibility.Public => true,
                Accessibility.Protected => includeEffectivelyPrivateSymbols || symbol.ContainingType == null || !IsEffectivelySealed(symbol.ContainingType, includeInternalSymbols),
                Accessibility.ProtectedOrInternal => includeEffectivelyPrivateSymbols || includeInternalSymbols || symbol.ContainingType == null || !IsEffectivelySealed(symbol.ContainingType, includeInternalSymbols),
                Accessibility.ProtectedAndInternal => includeInternalSymbols && (includeEffectivelyPrivateSymbols || symbol.ContainingType == null || !IsEffectivelySealed(symbol.ContainingType, includeInternalSymbols)),
                Accessibility.Private => includeExplicitInterfaceImplementationSymbols && IsExplicitInterfaceImplementation(symbol),
                _ => includeInternalSymbols,
            };

        public static bool IsEventAdderOrRemover(this IMethodSymbol method) =>
            method.MethodKind == MethodKind.EventAdd ||
            method.MethodKind == MethodKind.EventRemove ||
            method.Name.StartsWith("add_", StringComparison.Ordinal) ||
            method.Name.StartsWith("remove_", StringComparison.Ordinal);
    }
}
