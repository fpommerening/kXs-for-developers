using System;
using System.ComponentModel;

namespace FP.ContainerTraining.EventOperator.Models
{
    public class EventPortalModel
    {
        public string Name { get; set; }

        public string Namespace { get; set; }

        public string Server { get; set; }

        [DisplayName("Base-Url")]
        public string BaseUrl { get; set; }

        public DateTime? CreationTimestamp { get; set; }

       
    }
}
