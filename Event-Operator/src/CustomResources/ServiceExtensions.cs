using Microsoft.Extensions.DependencyInjection;

namespace FP.ContainerTraining.EventOperator.CustomResources
{
    public static class ServiceExtensions
    {
        public static void AddCustomResources(this IServiceCollection services)
        {
            services.AddSingleton(new CustomResourceDefinition<EventPortal>("crd.container-training.de", "v1", "eventportals", "eventportal", "EventPortal"));
            services.AddSingleton(new CustomResourceDefinition<IngressRoute>("traefik.containo.us", "v1alpha1", "ingressroutes", "ingressroute", "IngressRoute"));
        }

        public static void AddCustomResourceHandler(this IServiceCollection services)
        {
            services.AddTransient<EventPortalHandler>();
        }

    }

}
