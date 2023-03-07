using System.Text.Json;
using FP.ContainerTraining.EventOperator.Business;
using k8s;
using k8s.Autorest;

namespace FP.ContainerTraining.EventOperator.CustomResources;

public class CustomResourceDefinition<T> where T : CustomResource
    {
        private Watcher<T>? _watcher;

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

        public async Task<T?> GetObjectOrDefaultAsync(IKubernetes kubernetes, string name, string @namespace = "default")
        {
            try
            {
                var result = await kubernetes.CustomObjects.GetNamespacedCustomObjectAsync(Group, Version, @namespace,
                    Plural, name);
                if (result is JsonElement resultElement)
                {
                    return resultElement.Deserialize<T>(KubernetesSerialization.Options);
                }

                return default;
            }
            catch (HttpOperationException hoex) when (hoex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return default;
            }
        }

        public async Task<IReadOnlyList<T>> GetObjectsAsync(IKubernetes kubernetes, string @namespace = "default")
        {
            var result = await kubernetes.CustomObjects.ListNamespacedCustomObjectAsync(Group, Version, @namespace, Plural);
            if (result is JsonElement root && root.TryGetProperty("items", out var itemsProperty))
            {
                return itemsProperty.Deserialize<T[]>(KubernetesSerialization.Options) ?? Array.Empty<T>();
            }

            return Array.Empty<T>();
        }

        public async Task CreateObject(ICustomObjectsOperations customObjectsOperations, T obj, string @namespace = "default")
        {
            if (string.IsNullOrEmpty(obj.Kind))
            {
                obj.Kind = Kind;
            }

            if (string.IsNullOrEmpty(obj.ApiVersion))
            {
                obj.ApiVersion = $"{Group}/{Version}";
            }

            await customObjectsOperations.CreateNamespacedCustomObjectAsync(obj, Group, Version, @namespace, Plural);
        }
    }
