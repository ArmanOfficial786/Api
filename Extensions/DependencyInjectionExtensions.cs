using NexgenCosysReport.Services.ReportService;
using System.Reflection;

namespace NexgenCosysReport.Extensions;

public static class DependencyInjectionExtensions
{
    // Fully-qualified namespace roots to scan. Add new top-level folders here
    // (e.g. "NexgenCosysReport.Repository.Loan") only if they DON'T already
    // fall under one of these roots — subfolders are included automatically.
    private static readonly string[] ScanNamespaceRoots =
    {
        "NexgenCosysReport.Repository",
        "NexgenCosysReport.Services",
        "NexgenCosysAPI.Repository"
    };

    // Types that must be Singleton despite matching the default convention below.
    // Keep this list in sync manually — reflection can't infer lifetime intent.
    private static readonly HashSet<Type> SingletonOverrides = new()
    {
        typeof(RazorRenderService),
        typeof(JsReportService),
        typeof(ProgressivePdfService)
    };

    /// <summary>
    /// Scans the given assemblies for concrete classes whose namespace falls under
    /// Repository/* or Services/* (any depth, any subfolder) and registers them
    /// against every custom interface they implement. Default lifetime is Scoped;
    /// see SingletonOverrides for exceptions.
    /// </summary>
    public static IServiceCollection AddRepositoriesAndServices(
        this IServiceCollection services,
        params Assembly[] assemblies)
    {
        var candidates = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass
                        && !t.IsAbstract
                        && t.Namespace is not null
                        && ScanNamespaceRoots.Any(root =>
                               t.Namespace == root || t.Namespace.StartsWith(root + ".")));

        foreach (var implType in candidates)
        {
            var interfaces = implType.GetInterfaces()
                .Where(i => i.Namespace is not null
                            && (i.Namespace.StartsWith("NexgenCosysReport")
                                || i.Namespace.StartsWith("NexgenCosysAPI")))
                .ToList();

            if (interfaces.Count == 0)
                continue; // no custom interface — leave for manual registration (e.g. CustomHeaderResponse)

            var lifetime = SingletonOverrides.Contains(implType)
                ? ServiceLifetime.Singleton
                : ServiceLifetime.Scoped;

            foreach (var iface in interfaces)
                services.Add(new ServiceDescriptor(iface, implType, lifetime));
        }

        return services;
    }
}