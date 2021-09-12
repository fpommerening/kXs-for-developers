using System.Collections.Generic;
using System.ComponentModel;

namespace FP.ContainerTraining.EventOperator.Models
{
    public class EventPortalDetailModel : EventPortalModel
    {
        [DisplayName("User")]
        public string SshUser { get; set; }

        [DisplayName("Password")]
        public string SshPassword { get; set; }

        [DisplayName("Password")]
        public string PortalPassword { get; set; }

        public EventPortalAppModel ShellInABox { get; set; }

        public EventPortalAppModel CodeServer { get; set; }

        public List<KubernetesMetaModel> ObjectsInNamespace { get; } = new List<KubernetesMetaModel>();
    }
}
