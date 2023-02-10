namespace FP.ContainerTraining.EventOperator.CustomResources;

public class IngressRoute : CustomResource<IngressRouteSpec>
{
}

public class IngressRouteSpec
{
    public string[] EntryPoints { get; set; }

    public Route[] Routes { get; set; }

    public Security Tls { get; set; }

    public class Route
    {
        public string Kind { get; set; }

        public int Priority { get; set; }

        public string Match { get; set; }

        public Service[] Services { get; set; }
    }

    public class Service
    {
        public string Name { get; set; }
        public int Port { get; set; }
    }

    public class Security
    {
        public string CertResolver { get; set; }
    }
}