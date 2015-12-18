using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNet.Mvc.Razor;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyModel;
using Microsoft.AspNet.Mvc.Razor.Compilation;

using CompilationOptions = Microsoft.Extensions.DependencyModel.CompilationOptions;
using Platform = Microsoft.CodeAnalysis.Platform;

namespace HelloMvc
{
    public class MvcCompilationOptions : ConfigureOptions<RazorViewEngineOptions>
    {
        public MvcCompilationOptions() : base(options => { })
        {
        }

        private Lazy<IEnumerable<MetadataReference>> _references = new Lazy<IEnumerable<MetadataReference>>(GetReferences);

        private Lazy<CompilationOptions> _compilationOptions = new Lazy<CompilationOptions>(GetCompilationOptions);

        public override void Configure(RazorViewEngineOptions options)
        {
            var previousCallback = options.CompilationCallback;
            var compilationOptions = _compilationOptions.Value;

            SetParseOptions(options, compilationOptions);

            options.CompilationCallback = (context) =>
            {
                previousCallback?.Invoke(context);

                SetCompilationOptions(context, compilationOptions);
            };
        }

        private void SetCompilationOptions(RoslynCompilationContext context, CompilationOptions compilationOptions)
        {
            var roslynOptions = context.Compilation.Options;

            // Disable 1702 until roslyn turns this off by default
            roslynOptions = roslynOptions.WithSpecificDiagnosticOptions(
                new Dictionary<string, ReportDiagnostic>
                {
                    {"CS1701", ReportDiagnostic.Suppress}, // Binding redirects
                    {"CS1702", ReportDiagnostic.Suppress},
                    {"CS1705", ReportDiagnostic.Suppress}
                });

            if (compilationOptions.AllowUnsafe.HasValue)
            {
                roslynOptions = roslynOptions.WithAllowUnsafe(compilationOptions.AllowUnsafe.Value);
            }

            if (compilationOptions.Optimize.HasValue)
            {
                var optimizationLevel = compilationOptions.Optimize.Value ? OptimizationLevel.Debug : OptimizationLevel.Release;
                roslynOptions = roslynOptions.WithOptimizationLevel(optimizationLevel);
            }

            if (compilationOptions.WarningsAsErrors.HasValue)
            {
                var reportDiagnostic = compilationOptions.WarningsAsErrors.Value ? ReportDiagnostic.Error : ReportDiagnostic.Default;
                roslynOptions = roslynOptions.WithGeneralDiagnosticOption(reportDiagnostic);
            }

            context.Compilation = context.Compilation
                .WithReferences(_references.Value)
                .WithOptions(roslynOptions);
        }

        private static void SetParseOptions(RazorViewEngineOptions options, CompilationOptions compilationOptions)
        {
            var roslynParseOptions = options.ParseOptions;
            roslynParseOptions = roslynParseOptions.WithPreprocessorSymbols(compilationOptions.Defines);

            var languageVersion = roslynParseOptions.LanguageVersion;
            if (Enum.TryParse(compilationOptions.LanguageVersion, ignoreCase: true, result: out languageVersion))
            {
                roslynParseOptions = roslynParseOptions.WithLanguageVersion(languageVersion);
            }

            options.ParseOptions = roslynParseOptions;
        }

        private static IEnumerable<MetadataReference> GetReferences()
        {
            var entryAssembly = (Assembly) typeof(Assembly).GetTypeInfo().GetDeclaredMethod("GetEntryAssembly").Invoke(null, null);

            var location = Path.Combine(Path.GetDirectoryName(entryAssembly.Location), "refs");

            return Directory.GetFiles(location, "*.dll").Select(file => MetadataReference.CreateFromFile(file));
        }

        private static CompilationOptions GetCompilationOptions()
        {
            return DependencyContext.Load().CompilationOptions;
        }
    }
}