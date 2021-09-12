using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FP.ContainerTraining.EventOperator.CustomResources
{
    public class EventPortal : CustomResource<EventPortalSpec>
    { 
    }

    public class EventPortalSpec
    {
        public string Namespace { get; set; }

        public string Server { get; set; }

        public string SshUser { get; set; }

        public string SshPassword { get; set; }

        public string PortalPassword { get; set; }

        public string BaseUrl { get; set; }

        public EventPortalApplicationSpec ShellInABox { get; set; } = new EventPortalApplicationSpec
        {
            Image = "fpommerening/container-training:shellinabox",
            Prefix = "console",
            Port = 4200
        };

        public EventPortalApplicationSpec CodeServer { get; set; } = new EventPortalApplicationSpec
        {
            Image = "fpommerening/container-training:code-server",
            Prefix = "code",
            Port = 8080

        };
    }

    public class EventPortalApplicationSpec
    {
        public string Prefix { get; set; }

        public string Image { get; set; }

        public int Port { get; set; }
    }
}
