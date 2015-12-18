using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.Extensions.DependencyModel;

namespace HelloMvc
{
    internal class DependencyContextAssemblyProvider: IAssemblyProvider
    {
        private const string MicrosoftAspnetMvcPrefix = "Microsoft.AspNet.Mvc";

        private Lazy<IEnumerable<Assembly>> _candidateAssemblies = new Lazy<IEnumerable<Assembly>>(GetCandidateAssemblies);

        private static IEnumerable<Assembly> GetCandidateAssemblies()
        {
            var dependencyContext = DependencyContext.Load();
            var dependsOnMvc = dependencyContext.RuntimeLibraries.Where(DependsOnMvc);

            foreach (var library in dependsOnMvc)
            {
                if (library.PackageName.StartsWith(MicrosoftAspnetMvcPrefix))
                {
                    // skip mvc libs
                    continue;
                }

                foreach (var assembly in library.Assemblies)
                {
                    yield return Assembly.Load(new AssemblyName(Path.GetFileNameWithoutExtension(assembly)));
                }
            }
        }

        private static bool DependsOnMvc(Library library)
        {
            return library.Dependencies.Any(dependency => dependency.Name.StartsWith(MicrosoftAspnetMvcPrefix));
        }

        public IEnumerable<Assembly> CandidateAssemblies => _candidateAssemblies.Value;
    }
}