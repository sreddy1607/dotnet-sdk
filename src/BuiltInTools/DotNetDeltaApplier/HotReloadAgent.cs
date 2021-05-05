﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Watcher.Tools;

namespace Microsoft.Extensions.HotReload
{
    internal class HotReloadAgent : IDisposable
    {
        private readonly Action<string> _log;
        private readonly AssemblyLoadEventHandler _assemblyLoad;
        private readonly ConcurrentDictionary<Guid, IReadOnlyList<UpdateDelta>> _deltas = new();
        private readonly ConcurrentDictionary<Assembly, Assembly> _appliedAssemblies = new();
        private volatile UpdateHandlerActions? _handlerActions;

        public HotReloadAgent(Action<string> log)
        {
            _log = log;
            _assemblyLoad = OnAssemblyLoad;
            AppDomain.CurrentDomain.AssemblyLoad += _assemblyLoad;
        }

        private void OnAssemblyLoad(object? _, AssemblyLoadEventArgs eventArgs)
        {
            _handlerActions = null;
            var loadedAssembly = eventArgs.LoadedAssembly;
            var moduleId = loadedAssembly.Modules.FirstOrDefault()?.ModuleVersionId;
            if (moduleId is null)
            {
                return;
            }

            if (_deltas.TryGetValue(moduleId.Value, out var updateDeltas) && _appliedAssemblies.TryAdd(loadedAssembly, loadedAssembly))
            {
                // A delta for this specific Module exists and we haven't called ApplyUpdate on this instance of Assembly as yet.
                ApplyDeltas(updateDeltas);
            }
        }

        internal sealed class UpdateHandlerActions
        {
            public List<Action<Type[]?>> Before { get; } = new();
            public List<Action<Type[]?>> After { get; } = new();
            public List<Action<Type[]?>> ClearCache { get; } = new();
            public List<Action<Type[]?>> UpdateApplication { get; } = new();
        }

        private UpdateHandlerActions GetMetadataUpdateHandlerActions()
        {
            // In a typical app, a handful of assemblies will have metadata handler attributes. We need to discover these assemblies
            // and ensure that they are topologically sorted so that handlers in a dependency are executed before the dependent (e.g.
            // the reflection cache action in System.Private.CoreLib is executed before System.Text.Json clears it's own cache.)
            // This would ensure that there is a well-defined order and that caches and updates more lower in the application stack
            // are up to date before ones higher in the stack are recomputed.
            var assemblies = GetAssembliesWithMetadataUpdateHandlerAttributes();
            var sortedAssemblies = TopologicalSort(CollectionsMarshal.AsSpan(assemblies));
            var handlerActions = new UpdateHandlerActions();
            foreach (var assembly in CollectionsMarshal.AsSpan(sortedAssemblies))
            {
                foreach (var attribute in assembly.GetCustomAttributes<MetadataUpdateHandlerAttribute>())
                {
                    GetHandlerActions(handlerActions, attribute.HandlerType);
                }
            }

            return handlerActions;

            static List<Assembly> GetAssembliesWithMetadataUpdateHandlerAttributes()
            {
                var assemblies = new List<Assembly>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (assembly.IsDefined(typeof(MetadataUpdateHandlerAttribute)))
                    {
                        assemblies.Add(assembly);
                    }
                }

                return assemblies;
            }
        }

        internal void GetHandlerActions(UpdateHandlerActions handlerActions, Type handlerType)
        {
            bool methodFound = false;

            // Temporarily allow BeforeUpdate and AfterUpdate to be invoked until
            // everything is updated to use the new names.
            if (GetUpdateMethod(handlerType, "BeforeUpdate") is MethodInfo beforeUpdate)
            {
                handlerActions.Before.Add(CreateAction(beforeUpdate));
                methodFound = true;
            }

            if (GetUpdateMethod(handlerType, "AfterUpdate") is MethodInfo afterUpdate)
            {
                handlerActions.After.Add(CreateAction(afterUpdate));
                methodFound = true;
            }

            if (GetUpdateMethod(handlerType, "ClearCache") is MethodInfo clearCache)
            {
                handlerActions.ClearCache.Add(CreateAction(clearCache));
                methodFound = true;
            }

            if (GetUpdateMethod(handlerType, "UpdateApplication") is MethodInfo updateApplication)
            {
                handlerActions.UpdateApplication.Add(CreateAction(updateApplication));
                methodFound = true;
            }

            if (!methodFound)
            {
                _log($"No invokable methods found on metadata handler type '{handlerType}'. " +
                    $"Allowed methods are ClearCache, UpdateApplication");
            }

            Action<Type[]?> CreateAction(MethodInfo update)
            {
                Action<Type[]?> action = update.CreateDelegate<Action<Type[]?>>();
                return types =>
                {
                    try
                    {
                        action(types);
                    }
                    catch (Exception ex)
                    {
                        _log($"Exception from '{action}': {ex}");
                    }
                };
            }

            MethodInfo? GetUpdateMethod(Type handlerType, string name)
            {
                if (handlerType.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, new[] { typeof(Type[]) }) is MethodInfo updateMethod &&
                    updateMethod.ReturnType == typeof(void))
                {
                    return updateMethod;
                }

                foreach (MethodInfo method in handlerType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance))
                {
                    if (method.Name == name)
                    {
                        _log($"Type '{handlerType}' has method '{method}' that does not match the required signature.");
                        break;
                    }
                }

                return null;
            }
        }

        internal static List<Assembly> TopologicalSort(ReadOnlySpan<Assembly> assemblies)
        {
            var sortedAssemblies = new List<Assembly>(assemblies.Length);

            var visited = new HashSet<string>(StringComparer.Ordinal);

            foreach (var assembly in assemblies)
            {
                Visit(assemblies, assembly, sortedAssemblies, visited);
            }

            static void Visit(ReadOnlySpan<Assembly> assemblies, Assembly assembly, List<Assembly> sortedAssemblies, HashSet<string> visited)
            {
                var assemblyIdentifier = assembly.FullName ?? assembly.ToString();
                if (!visited.Add(assemblyIdentifier))
                {
                    return;
                }

                foreach (var dependencyName in assembly.GetReferencedAssemblies())
                {
                    var dependency = FindDependency(assemblies, dependencyName);
                    if (dependency is not null)
                    {
                        Visit(assemblies, dependency, sortedAssemblies, visited);
                    }
                }

                sortedAssemblies.Add(assembly);
            }

            static Assembly? FindDependency(ReadOnlySpan<Assembly> assemblies, AssemblyName dependencyName)
            {
                foreach (var assembly in assemblies)
                {
                    if (assembly.FullName == dependencyName.FullName)
                    {
                        return assembly;
                    }
                }

                return null;
            }

            return sortedAssemblies;
        }

        public void ApplyDeltas(IReadOnlyList<UpdateDelta> deltas)
        {
            try
            {
                // Defer discovering the receiving deltas until the first hot reload delta.
                // This should give enough opportunity for AppDomain.GetAssemblies() to be sufficiently populated.
                _handlerActions ??= GetMetadataUpdateHandlerActions();
                var handlerActions = _handlerActions;

                // TODO: Get types to pass in
                Type[]? updatedTypes = null;

                InvokeActions(handlerActions.Before, updatedTypes);

                for (var i = 0; i < deltas.Count; i++)
                {
                    var item = deltas[i];
                    var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.Modules.FirstOrDefault() is Module m && m.ModuleVersionId == item.ModuleId);
                    if (assembly is not null)
                    {
                        System.Reflection.Metadata.AssemblyExtensions.ApplyUpdate(assembly, item.MetadataDelta, item.ILDelta, ReadOnlySpan<byte>.Empty);
                    }
                }

                InvokeActions(handlerActions.ClearCache, updatedTypes);
                InvokeActions(handlerActions.After, updatedTypes);
                InvokeActions(handlerActions.UpdateApplication, updatedTypes);

                _log("Deltas applied.");
            }
            catch (Exception ex)
            {
                _log(ex.ToString());
            }

            static void InvokeActions(List<Action<Type[]?>> actions, Type[]? updatedTypes)
            {
                foreach (var action in CollectionsMarshal.AsSpan(actions))
                {
                    action(updatedTypes);
                }
            }
        }

        public void Dispose()
        {
            AppDomain.CurrentDomain.AssemblyLoad -= _assemblyLoad;
        }
    }
}
