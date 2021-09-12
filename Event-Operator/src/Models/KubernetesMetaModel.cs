using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace FP.ContainerTraining.EventOperator.Models
{
    public class KubernetesMetaModel
    {
        public string Name { get; set; }

        public string ApiVersion { get; set; }

        public string Kind { get; set; }

        public DateTime? CreationTimestamp { get; set; }
    }
}
