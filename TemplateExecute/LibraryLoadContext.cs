//---------------------------------------------//
// Copyright 2023 RdJNL                        //
// https://github.com/RdJNL/TextTemplatingCore //
//---------------------------------------------//
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.DependencyModel.Resolution;


namespace RdJNL.TemplateExecute
{
    internal sealed class LibraryLoadContext : AssemblyLoadContext
    {
        public Assembly LoadLibrary(string library)
        {
            Assembly assembly;
            if( library.EndsWith(".dll") )
            {
                assembly = LoadFromAssemblyPath(library);
            }
            else
            {
                assembly = LoadFromAssemblyName(new AssemblyName(library));
            }

            DependencyContext? dependencyContext = DependencyContext.Load(assembly);
            var resolver = new CompositeCompilationAssemblyResolver(
            [
                new AppBaseCompilationAssemblyResolver(Path.GetDirectoryName(library)!),
                new ReferenceAssemblyPathResolver(),
                new PackageCompilationAssemblyResolver(),
            ]);

            Assembly? _OnResolving(AssemblyLoadContext context, AssemblyName name)
            {
                if( dependencyContext == null )
                {
                    return null;
                }

                RuntimeLibrary? library = dependencyContext.RuntimeLibraries.FirstOrDefault(rl => rl.Name.Equals(name.Name, StringComparison.OrdinalIgnoreCase));

                if( library != null )
                {
                    var wrapper = new CompilationLibrary(library.Type, library.Name, library.Version, library.Hash,
                        library.RuntimeAssemblyGroups.SelectMany(rag => rag.AssetPaths), library.Dependencies, library.Serviceable);

                    var assemblies = new List<string>();
                    resolver.TryResolveAssemblyPaths(wrapper, assemblies);
                    if( assemblies.Count > 0 )
                    {
                        return LoadFromAssemblyPath(assemblies[0]);
                    }
                }

                return null;
            }

            Resolving += _OnResolving;

            return assembly;
        }
    }
}
