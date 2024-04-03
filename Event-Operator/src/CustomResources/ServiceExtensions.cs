using k8s;

namespace FP.ContainerTraining.EventOperator.CustomResources;

public static class ServiceExtensions
{
    public static void AddCustomResources(this IServiceCollection services)
    {
        services.AddSingleton(new CustomResourceDefinition<EventPortal>("crd.container-training.de", "v1", "eventportals", "eventportal", "EventPortal"));
        services.AddSingleton(new CustomResourceDefinition<IngressRoute>("traefik.io", "v1alpha1", "ingressroutes", "ingressroute", "IngressRoute"));
    }

    public static void AddCustomResourceHandler(this IServiceCollection services)
    {
        services.AddTransient<EventPortalHandler>();
    }
    
    public static void AddCustomResourceWatcher(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<Watcher<EventPortal>>(provider =>
        {
            var kubernetes = provider.GetRequiredService<IKubernetes>();
            var crd = provider.GetRequiredService<CustomResourceDefinition<EventPortal>>();
            var handler = provider.GetRequiredService<EventPortalHandler>();
            var @namespace = configuration["PortalNamespace"] ?? "default";
            var logger = provider.GetRequiredService<ILogger<Watcher<EventPortal>>>();
            return new Watcher<EventPortal>(kubernetes, crd, handler,logger, @namespace);
        });
    }

}