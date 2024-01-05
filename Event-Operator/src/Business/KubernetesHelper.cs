using FP.ContainerTraining.EventOperator.CustomResources;
using k8s;
using k8s.Models;

namespace FP.ContainerTraining.EventOperator.Business;

public class KubernetesHelper
{
    public static async Task CreateNamespace(IKubernetes kubernetes, string name, Dictionary<string, string> labels)
    {
        if (!await NamespaceExists(kubernetes, name, new Dictionary<string, string>()))
        {
            var @namespace = new V1Namespace
            {
                Metadata = new V1ObjectMeta
                {
                    Name = name,
                    Labels = new Dictionary<string, string>(labels)
                }
            };
            await kubernetes.CoreV1.CreateNamespaceAsync(@namespace);
        }
    }

    public static async Task<bool> NamespaceExists(IKubernetes kubernetes, string name,
        Dictionary<string, string> labels)
    {
        var namespaces = await kubernetes.CoreV1.ListNamespaceAsync();
        return namespaces.Items.Any(x => Match(x.Metadata, name, labels));
    }

    public static async Task DeleteNamespace(IKubernetes kubernetes, string name)
    {
        await kubernetes.CoreV1.DeleteNamespaceAsync(name);
    }

    public static async Task CreatePersistentVolumeClaim(IKubernetes kubernetes, string name, string @namespace,
        Dictionary<string, string> labels, int sizeInMiByte, string storageClassName = "local-path")
    {
        var claims = await kubernetes.CoreV1.ListNamespacedPersistentVolumeClaimAsync(@namespace);
        if (claims.Items.Any(x => Match(x.Metadata, name, labels)))
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
                        { "storage", new ResourceQuantity($"{sizeInMiByte}Mi") }
                    }
                },
                AccessModes = new List<string> { "ReadWriteOnce" },
                StorageClassName = storageClassName
            }
        };
        await kubernetes.CoreV1.CreateNamespacedPersistentVolumeClaimAsync(claim, @namespace);
    }

    public static async Task CreateSecret(IKubernetes kubernetes, string name, string @namespace,
        Dictionary<string, string> labels, Dictionary<string, byte[]> data, string type = "Opaque")
    {
        var secrets = await kubernetes.CoreV1.ListNamespacedSecretAsync(@namespace);
        if (secrets.Items.Any(x => Match(x.Metadata, name, labels)))
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

        await kubernetes.CoreV1.CreateNamespacedSecretAsync(secret, @namespace);
    }

    public static async Task CopySecret(IKubernetes kubernetes,
        Dictionary<string, string> sourceLabels,
        string sourceNamespace,
        string sourceName,
        Dictionary<string, string> targetLabels,
        string targetNamespace,
        string targetName
    )
    {
        var targetSecrets = await kubernetes.CoreV1.ListNamespacedSecretAsync(targetNamespace);
        if (targetSecrets.Items.Any(x => Match(x.Metadata, targetName, targetLabels)))
        {
            return;
        }

        var sourceSecrets = await kubernetes.CoreV1.ListNamespacedSecretAsync(sourceNamespace);
        var sourceSecret = sourceSecrets.Items.Single(x => Match(x.Metadata, sourceName, sourceLabels));
        var targetSecret = new V1Secret
        {
            Data = sourceSecret.Data,
            Type = sourceSecret.Type,
            Metadata = new V1ObjectMeta
            {
                Name = targetName,
                Labels = targetLabels
            }
        };
        await kubernetes.CoreV1.CreateNamespacedSecretAsync(targetSecret, @targetNamespace);
    }

    public static async Task CreateConfigMap(IKubernetes kubernetes, string name, string @namespace,
        Dictionary<string, string> labels, Dictionary<string, string> data)
    {
        var configMaps = await kubernetes.CoreV1.ListNamespacedConfigMapAsync(@namespace);
        if (configMaps.Items.Any(x => Match(x.Metadata, name, labels)))
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
        await kubernetes.CoreV1.CreateNamespacedConfigMapAsync(configMap, @namespace);
    }

    public static async Task CreateExternalName(IKubernetes kubernetes, string name, string @namespace,
        Dictionary<string, string> labels, string target)
    {
        var services = await kubernetes.CoreV1.ListNamespacedServiceAsync(@namespace);
        if (services.Items.Any(x => Match(x.Metadata, name, labels)))
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
        await kubernetes.CoreV1.CreateNamespacedServiceAsync(service, @namespace);
    }

    public static async Task CreateClusterIpService(IKubernetes kubernetes, string name, string @namespace,
        Dictionary<string, string> labels, Dictionary<string, string> selector, List<(string name, int port, int target)> ports)
    {
        var services = await kubernetes.CoreV1.ListNamespacedServiceAsync(@namespace);
        if (services.Items.Any(x => Match(x.Metadata, name, labels)))
        {
            return;
        }

        var servicePorts = ports.Select(port => new V1ServicePort
            { Port = port.port, Name = port.name, Protocol = "TCP", TargetPort = port.target }).ToList();

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
        await kubernetes.CoreV1.CreateNamespacedServiceAsync(service, @namespace);
    }

    public static async Task CreateIngressRoute(IKubernetes kubernetes, string name, string @namespace,
        Dictionary<string, string> labels, CustomResourceDefinition<IngressRoute> crd, string entryPoint,
        string match, int priority, Dictionary<string, int> services, string ingressClass = "", string certResolver = "")
    {
        var existingRoutes = await crd.GetObjectsAsync(kubernetes, @namespace);
        if (existingRoutes.Any(x => Match(x.Metadata, name, labels)))
        {
            return;
        }

        var annotations = new Dictionary<string, string>();
        if (!string.IsNullOrEmpty(ingressClass))
        {
            annotations.Add("kubernetes.io/ingress.class", ingressClass);
        }
        
        var route = new IngressRoute
        {
            Metadata = new V1ObjectMeta
            {
                Name = name,
                Labels = labels,
                Annotations = annotations
                
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
                        Services = services.Select(x => new IngressRouteSpec.Service { Name = x.Key, Port = x.Value })
                            .ToArray()
                    }
                },
                EntryPoints = new[] { entryPoint }
            }
        };

        if (!string.IsNullOrEmpty(certResolver))
        {
            route.Spec.Tls = new IngressRouteSpec.Security
            {
                CertResolver = certResolver
            };
        }

        await crd.CreateObject(kubernetes.CustomObjects, route, @namespace);
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
