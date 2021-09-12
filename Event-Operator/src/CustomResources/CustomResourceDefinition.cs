using System.Collections.Generic;
using System.Threading.Tasks;
using FP.ContainerTraining.EventOperator.Business;
using k8s;
using Microsoft.Rest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FP.ContainerTraining.EventOperator.CustomResources
{
    public class CustomResourceDefinition<T> where T : CustomResource
    {
        private Watcher<T> _watcher;

        public CustomResourceDefinition(string group, string version, string plural, string singular, string kind)
        {
            Group = group;
            Version = version;
            Plural = plural;
            Singular = singular;
            Kind = kind;
        }

        public string Group { get; protected set; }
        public string Version { get; protected set; }
        public string Plural { get; protected set; }
        public string Singular { get; protected set; }
        public string Kind { get; protected set; }

        public async Task<T> GetObjectOrDefaultAsync(Kubernetes kubernetes, string name, string @namespace = "default")
        {
            try
            {
                var result =
                    await kubernetes.GetNamespacedCustomObjectWithHttpMessagesAsync(Group, Version, @namespace, Plural,
                        name);
                var js = result.Body as JObject;
                return js?.ToObject<T>();
            }
            catch (HttpOperationException hoex) when (hoex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default(T);
            }
        }

        public async Task<IReadOnlyList<T>> GetObjectsAsync(Kubernetes kubernetes, string @namespace = "default")
        {
            var result =
                await kubernetes.ListNamespacedCustomObjectWithHttpMessagesAsync(Group, Version, @namespace, Plural);
            var js = result.Body as JObject;
            var array = js?.GetValue("items") as JArray;
            if (array == null)
            {
                return new T[] { };
            }

            return array.ToObject<List<T>>().AsReadOnly();
        }

        public async Task CreateObject(Kubernetes kubernetes, T obj, string @namespace = "default")
        {
            if (string.IsNullOrEmpty(obj.Kind))
            {
                obj.Kind = Kind;
            }

            if (string.IsNullOrEmpty(obj.ApiVersion))
            {
                obj.ApiVersion = $"{Group}/{Version}";
            }

            var js = JObject.FromObject(obj, JsonSerializer.Create(KubernetesSerialization.SerializerSettings));

            await kubernetes.CreateNamespacedCustomObjectWithHttpMessagesAsync(js, Group, Version, @namespace, Plural);
            
        }

        public void Watch(Kubernetes kubernetes, ICustomerResourceHandler<T> handler, string @namespace = "default")
        {
            if (_watcher == null)
            {
                _watcher = new Watcher<T>(kubernetes, this, handler, @namespace);
            }

            _watcher.Ensure();
        }
    }


}
