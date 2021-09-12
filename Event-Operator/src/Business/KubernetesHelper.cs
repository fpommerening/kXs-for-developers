using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FP.ContainerTraining.EventOperator.CustomResources;
using k8s;
using k8s.Models;

namespace FP.ContainerTraining.EventOperator.Business
{
    public class KubernetesHelper
    {
        public static async Task CreateNamespace(Kubernetes client, string name, Dictionary<string, string> labels)
        {
            if (!await NamespaceExists(client, name, new Dictionary<string, string>()))
            {
                var @namespace = new V1Namespace
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = name,
                        Labels = new Dictionary<string, string>(labels)
                    }
                };
                await client.CreateNamespaceAsync(@namespace);
            }
        }

        public static async Task<bool> NamespaceExists(Kubernetes client, string name,
            Dictionary<string, string> labels)
        {
            var namespaces = await client.ListNamespaceAsync();
            return namespaces.Items.Any(x => Match(x.Metadata, name, labels));
        }

        public static async Task DeleteNamespace(Kubernetes client, string name)
        {
            await client.DeleteNamespaceAsync(name);
        }

        public static async Task CreatePersistentVolumeClaim(Kubernetes client, string name, string @namespace,
            Dictionary<string, string> labels, int sizeInMiByte, string storageClassName = "local-path")
        {
            var claims = await client.ListNamespacedPersistentVolumeClaimWithHttpMessagesAsync(@namespace);
            if (claims.Body.Items.Any(x => Match(x.Metadata, name, labels)))
            {
                return;
            }

            var claim = new V1PersistentVolumeClaim
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels
                },
                Spec = new V1PersistentVolumeClaimSpec
                {
                    Resources = new V1ResourceRequirements
                    {
                        Requests = new Dictionary<string, ResourceQuantity>
                        {
                            {"storage", new ResourceQuantity($"{sizeInMiByte}Mi")}
                        }
                    },
                    AccessModes = new List<string> {"ReadWriteOnce"},
                    StorageClassName = storageClassName
                }
            };
            await client.CreateNamespacedPersistentVolumeClaimWithHttpMessagesAsync(claim, @namespace);

        }

        public static async Task CreateSecret(Kubernetes client, string name, string @namespace,
            Dictionary<string, string> labels, Dictionary<string, byte[]> data, string type = "Opaque")
        {
            var secrets = await client.ListNamespacedSecretWithHttpMessagesAsync(@namespace);
            if (secrets.Body.Items.Any(x => Match(x.Metadata, name, labels)))
            {
                return;
            }

            var secret = new V1Secret
            {
                Data = data,
                Type = type,
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels
                }
            };

            await client.CreateNamespacedSecretWithHttpMessagesAsync(secret, @namespace);
        }

        public static async Task CreateConfigMap(Kubernetes client, string name, string @namespace,
            Dictionary<string, string> labels, Dictionary<string, string> data)
        {
            var configMaps = await client.ListNamespacedConfigMapWithHttpMessagesAsync(@namespace);
            if (configMaps.Body.Items.Any(x => Match(x.Metadata, name, labels)))
            {
                return;
            }

            var configMap = new V1ConfigMap()
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels
                },
                Data = data
            };
            await client.CreateNamespacedConfigMapWithHttpMessagesAsync(configMap, @namespace);
        }

        public static async Task CreateExternalName(Kubernetes client, string name, string @namespace,
            Dictionary<string, string> labels, string target)
        {
            var services = await client.ListNamespacedServiceWithHttpMessagesAsync(@namespace);
            if (services.Body.Items.Any(x => Match(x.Metadata, name, labels)))
            {
                return;
            }

            var service = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels
                },
                Spec = new V1ServiceSpec
                {
                    ExternalName = target,
                    Type = "ExternalName"
                }
            };
            await client.CreateNamespacedServiceWithHttpMessagesAsync(service, @namespace);
        }

        public static async Task CreateClusterIpService(Kubernetes client, string name, string @namespace,
            Dictionary<string, string> labels, Dictionary<string, string> selector, Dictionary<int, int> ports)
        {
            var services = await client.ListNamespacedServiceWithHttpMessagesAsync(@namespace);
            if (services.Body.Items.Any(x => Match(x.Metadata, name, labels)))
            {
                return;
            }

            var servicePorts = ports.Select(port => new V1ServicePort
                {Port = port.Key, Protocol = "TCP", TargetPort = port.Value}).ToList();

            var service = new V1Service
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels
                },
                Spec = new V1ServiceSpec
                {
                    Type = "ClusterIP",
                    Selector = selector,
                    Ports = servicePorts
                }
            };
            await client.CreateNamespacedServiceWithHttpMessagesAsync(service, @namespace);
        }

        public static async Task CreateIngressRoute(Kubernetes client, string name, string @namespace,
            Dictionary<string, string> labels, CustomResourceDefinition<IngressRoute> crd, string entryPoint,
            string match, int priority, Dictionary<string, int> services)
        {
            var existingRoutes = await crd.GetObjectsAsync(client, @namespace);
            if (existingRoutes.Any(x => Match(x.Metadata, name, labels)))
            {
                return;
            }

            var route = new IngressRoute
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = labels
                },
                Spec = new IngressRouteSpec
                {
                    Routes = new[]
                    {
                        new IngressRouteSpec.Route
                        {
                            Kind = "Rule",
                            Match = match,
                            Priority = priority,
                            Services = services.Select(x => new IngressRouteSpec.Service {Name = x.Key, Port = x.Value})
                                .ToArray()
                        }
                    }

                }
            };

            await crd.CreateObject(client, route, @namespace);
        }


        public static bool Match(V1ObjectMeta metadata, string name, Dictionary<string, string> labels)
        {
            if (metadata.Name != name)
            {
                return false;
            }

            foreach (var label in labels)
            {
                if (!metadata.Labels.TryGetValue(label.Key, out var value) || value != label.Value)
                {
                    return false;
                }
            }

            return true;
        }
    }
}