using System.Reflection;
using Microsoft.AspNet.Mvc.Infrastructure;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.CompilationAbstractions;

namespace HelloMvc
{
    public static class MvcServiceCollectionExtensions
    {
        /// Override the default AddMvc since we need to do some fix up for the CLI
        // This is temporary as more things come online
        public static IMvcBuilder AddMvc2(this IServiceCollection services)
        {
            services.AddSingleton<ILibraryExporter, NullExporter>();
            services.AddSingleton<IAssemblyProvider, DependencyContextAssemblyProvider>();
            services.AddSingleton<IConfigureOptions<RazorViewEngineOptions>, MvcCompilationOptions>();
            return services.AddMvc();
        }
    }
}