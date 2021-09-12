using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace FP.ContainerTraining.EventOperator.Business
{
    public class KubernetesSerialization
    {
        public static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false
                }
            }
        };
    }
}
