﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.DotNet.ApiCompatibility.Rules;

namespace Microsoft.DotNet.ApiCompatibility.Abstractions
{
    /// <summary>
    /// Object that represents a mapping between multiple <see cref="IAssemblySymbol"/> objects.
    /// This also holds a list of <see cref="INamespaceMapper"/> to represent the mapping of namespaces in between
    /// <see cref="IElementMapper{T}.Left"/> and <see cref="IElementMapper{T}.Right"/>.
    /// </summary>
    public class AssemblyMapper : ElementMapper<ElementContainer<IAssemblySymbol>>, IAssemblyMapper
    {
        private Dictionary<INamespaceSymbol, INamespaceMapper>? _namespaces;
        private readonly List<CompatDifference> _assemblyLoadErrors = new();

        /// <inheritdoc />
        public IAssemblySetMapper? ContainingAssemblySet { get; }

        /// <inheritdoc />
        public IEnumerable<CompatDifference> AssemblyLoadErrors => _assemblyLoadErrors;

        /// <summary>
        /// Instantiates an object with the provided <see cref="ComparingSettings"/>.
        /// </summary>
        /// <param name="settings">The settings used to diff the elements in the mapper.</param>
        /// <param name="rightSetSize">The number of elements in the right set to compare.</param>
        /// <param name="containingAssemblySet">The containing assembly set. Null, if the assembly isn't part of a set.</param>
        public AssemblyMapper(IRuleRunner ruleRunner,
            MapperSettings settings,
            int rightSetSize,
            IAssemblySetMapper? containingAssemblySet = null)
            : base(ruleRunner, settings, rightSetSize)
        {
            ContainingAssemblySet = containingAssemblySet;
        }

        /// <inheritdoc />
        public IEnumerable<INamespaceMapper> GetNamespaces()
        {
            if (_namespaces == null)
            {
                _namespaces = new Dictionary<INamespaceSymbol, INamespaceMapper>(Settings.EqualityComparer);
                AddOrCreateMappers(Left, ElementSide.Left);

                for (int i = 0; i < Right.Length; i++)
                {
                    AddOrCreateMappers(Right[i], ElementSide.Right, i);
                }

                void AddOrCreateMappers(ElementContainer<IAssemblySymbol>? assemblyContainer, ElementSide side, int setIndex = 0)
                {
                    // Silently return if the element hasn't been added yet.
                    if (assemblyContainer == null)
                    {
                        return;
                    }

                    Dictionary<INamespaceSymbol, List<INamedTypeSymbol>> typeForwards = ResolveTypeForwards(assemblyContainer, side, Settings.EqualityComparer, setIndex);

                    Queue<INamespaceSymbol> queue = new();
                    queue.Enqueue(assemblyContainer.Element.GlobalNamespace);
                    while (queue.Count > 0)
                    {
                        INamespaceSymbol nsSymbol = queue.Dequeue();
                        bool hasTypeForwards = typeForwards.TryGetValue(nsSymbol, out List<INamedTypeSymbol>? types);
                        if (hasTypeForwards || nsSymbol.GetTypeMembers().Length > 0)
                        {
                            INamespaceMapper mapper = AddMapper(nsSymbol);
                            if (hasTypeForwards)
                            {
                                mapper.AddForwardedTypes(types, side, setIndex);

                                // remove the typeforwards for this namespace as we did
                                // find and instance of the namespace on the current assembly
                                // and we don't want to create a mapper with the namespace in
                                // the assembly where the forwarded type is defined.
                                typeForwards.Remove(nsSymbol);
                            }
                        }

                        foreach (INamespaceSymbol child in nsSymbol.GetNamespaceMembers())
                            queue.Enqueue(child);
                    }

                    // If the current assembly didn't have a namespace symbol for the resolved typeforwards
                    // use the namespace symbol in the assembly where the forwarded type is defined.
                    // But create the mapper with typeForwardsOnly setting to not visit types defined in the target assembly.
                    foreach (KeyValuePair<INamespaceSymbol, List<INamedTypeSymbol>> kvp in typeForwards)
                    {
                        INamespaceMapper mapper = AddMapper(kvp.Key, checkIfExists: true, typeforwardsOnly: true);
                        mapper.AddForwardedTypes(kvp.Value, side, setIndex);
                    }

                    INamespaceMapper AddMapper(INamespaceSymbol ns, bool checkIfExists = false, bool typeforwardsOnly = false)
                    {
                        if (!_namespaces.TryGetValue(ns, out INamespaceMapper? mapper))
                        {
                            mapper = new NamespaceMapper(RuleRunner, Settings, Right.Length, this, typeforwardsOnly: typeforwardsOnly);
                            _namespaces.Add(ns, mapper);
                        }
                        else if (checkIfExists && mapper.GetElement(side, setIndex) != null)
                        {
                            return mapper;
                        }

                        mapper.AddElement(ns, side, setIndex);
                        return mapper;
                    }
                }

                Dictionary<INamespaceSymbol, List<INamedTypeSymbol>> ResolveTypeForwards(ElementContainer<IAssemblySymbol> assembly,
                    ElementSide side,
                    IEqualityComparer<ISymbol> comparer,
                    int index)
                {
                    Dictionary<INamespaceSymbol, List<INamedTypeSymbol>> typeForwards = new(comparer);
                    foreach (INamedTypeSymbol symbol in assembly.Element.GetForwardedTypes())
                    {
                        if (symbol.TypeKind != TypeKind.Error)
                        {
                            if (!typeForwards.TryGetValue(symbol.ContainingNamespace, out List<INamedTypeSymbol>? types))
                            {
                                types = new List<INamedTypeSymbol>();
                                typeForwards.Add(symbol.ContainingNamespace, types);
                            }

                            types.Add(symbol);
                        }
                        else
                        {
                            // If we should warn on missing references and we are unable to resolve the type forward, then we should log a diagnostic
                            if (Settings.WarnOnMissingReferences)
                            {
                                _assemblyLoadErrors.Add(new CompatDifference(
                                    side == ElementSide.Left ? assembly.MetadataInformation : MetadataInformation.DefaultLeft,
                                    side == ElementSide.Right ? assembly.MetadataInformation : MetadataInformation.DefaultRight,
                                    DiagnosticIds.AssemblyReferenceNotFound,
                                    string.Format(Resources.MatchingAssemblyNotFound, $"{symbol.ContainingAssembly.Name}.dll"),
                                    DifferenceType.Changed,
                                    symbol.ContainingAssembly.Identity.GetDisplayName()));
                            }
                        }
                    }

                    return typeForwards;
                }
            }

            return _namespaces.Values;
        }
    }
}
