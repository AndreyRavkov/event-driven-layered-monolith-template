using System.Reflection;
using AR.NetExt.Utils.Collections;
using Microsoft.Extensions.DependencyInjection;

namespace AR.Shared.Extensions;

public static class DIResolverExtensions
{
    public static void AddServicesFromAssemblyExt(this IServiceCollection services, Assembly assembly, string postfix)
    {
        var assemblyServices = assembly
                              .GetTypes()
                              .Where(type =>  type is { IsClass: true, IsAbstract: false, IsGenericType: false }
                                  && type.Name.EndsWith(postfix, StringComparison.OrdinalIgnoreCase));
        foreach (var service in assemblyServices)
        {
            var interfaces = service
                             .GetInterfaces()
                             .Where(x => x.Name.EndsWith(postfix, StringComparison.OrdinalIgnoreCase))
                             .AsReadOnlyListExt();
            if (!interfaces.Any())
            {
                continue;
            }

            // resolve interfaces
            foreach (var @interface in interfaces)
            {
                services.AddScoped(@interface, service);
            }
        }
    }
}
