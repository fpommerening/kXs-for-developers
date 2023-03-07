namespace FP.ContainerTraining.EventOperator.CustomResources;

public class IngressRoute : CustomResource<IngressRouteSpec>
{
}

public class IngressRouteSpec
{
    public string[] EntryPoints { get; set; } = Array.Empty<string>();

    public Route[] Routes { get; set; }  = Array.Empty<Route>();

    public Security? Tls { get; set; }

    public class Route
    {
        public string Kind { get; set; } = string.Empty;

        public int Priority { get; set; }

        public string Match { get; set; } = string.Empty;

        public Service[] Services { get; set; } = Array.Empty<Service>();
    }

    public class Service
    {
        public string Name { get; set; } = string.Empty;
        public int Port { get; set; }
    }

    public class Security
    {
        public string CertResolver { get; set; } = string.Empty;
    }
}