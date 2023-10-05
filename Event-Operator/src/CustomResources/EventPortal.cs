namespace FP.ContainerTraining.EventOperator.CustomResources;

public class EventPortal : CustomResource<EventPortalSpec>
{ 
}

public class EventPortalSpec
{
    public string Namespace { get; set; } = string.Empty;

    public string Server { get; set; } = string.Empty;

    public string SshUser { get; set; } = string.Empty;

    public string? SshPassword { get; set; }

    public string PortalPassword { get; set; } = string.Empty;

    public string BaseUrl { get; set; } = string.Empty;

    public EventPortalApplicationSpec ShellInABox { get; set; } = new EventPortalApplicationSpec
    {
        Image = "ghcr.io/fpommerening/shellinabox:latest",
        Prefix = "console",
        Port = 4200
    };

    public EventPortalApplicationSpec CodeServer { get; set; } = new EventPortalApplicationSpec
    {
        Image = "ghcr.io/fpommerening/codeserver:latest",
        Prefix = "code",
        Port = 8080

    };
}

public class EventPortalApplicationSpec
{
    public string Prefix { get; set; } = string.Empty;

    public string Image { get; set; } = string.Empty;

    public int Port { get; set; }
}