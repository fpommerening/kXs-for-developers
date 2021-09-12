using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FP.ContainerTraining.EventOperator.Business;
using k8s;
using k8s.Models;
using Microsoft.Extensions.Logging;

namespace FP.ContainerTraining.EventOperator.CustomResources
{
    public class EventPortalHandler : ICustomerResourceHandler<EventPortal>
    {
        private Dictionary<string, string> DefaultLabel = new Dictionary<string, string>
            {{"ControlledBy", "Community-Event-Operator"}};

        private readonly ILogger _logger;
        private readonly Kubernetes _kubernetes;
        private readonly CustomResourceDefinition<IngressRoute> _crdIngressRoute;

        public EventPortalHandler(ILogger<EventPortalHandler> logger, Kubernetes kubernetes, CustomResourceDefinition<IngressRoute> crdIngressRoute)
        {
            _logger = logger;
            _kubernetes = kubernetes;
            _crdIngressRoute = crdIngressRoute;
        }
        public async Task OnAdded(EventPortal crd)
        {
            try
            {
                var @namespace = crd.Spec.Namespace;
                var codeServerApp = crd.Spec.CodeServer;
                var shellInABoxApp = crd.Spec.ShellInABox;

                var pvcName = "portal-data-pvc";
                var cmName = "portal-code-server-cm";
                var secretName = "portal-kube-secret";
                var externalName = "portal-backend";
                var codeServerClusterIp = "portal-code-server-srv";
                var shellInABoxClusterIp = "portal-shell-in-a-box-srv";

                var codeServerRoute = "portal-code-server-ir";
                var shellInABoxRoute = "portal-shell-in-a-box-ir";
                var otherRoute = "portal-other-ir";

                var baseUrl = crd.Spec.BaseUrl;

                var codeServerSelector = new Dictionary<string, string>(DefaultLabel) { { "app", "code-server" } };
                var shellInABoxSelector = new Dictionary<string, string>(DefaultLabel) { { "app", "shell-in-a-box" } };

                var sbCodeServerConfig = new StringBuilder();
                sbCodeServerConfig.AppendLine("bind-addr: 127.0.0.1:8080");
                sbCodeServerConfig.AppendLine("auth: password");
                sbCodeServerConfig.AppendLine($"password: {crd.Spec.PortalPassword}");
                sbCodeServerConfig.AppendLine("cert: false");

                //var kubeLocalBytes = System.IO.File.ReadAllBytes(@"c:\temp\k3s.yaml");
                var kubeLocalBytes = await SshHelper.GetRemoteFile(crd.Spec.Server, crd.Spec.SshUser, crd.Spec.SshPassword, "/etc/rancher/k3s/k3s.yaml");
                var kubeText = System.Text.Encoding.UTF8.GetString(kubeLocalBytes).Replace("127.0.0.1", crd.Spec.Server);
                var kubeRemoteBytes = System.Text.Encoding.UTF8.GetBytes(kubeText);

                await KubernetesHelper.CreateNamespace(_kubernetes, @namespace, DefaultLabel);
                await KubernetesHelper.CreatePersistentVolumeClaim(_kubernetes, pvcName, @namespace, DefaultLabel, 2048);
                await KubernetesHelper.CreateConfigMap(_kubernetes, cmName, @namespace, DefaultLabel,
                    new Dictionary<string, string> { { "config.yaml", sbCodeServerConfig.ToString() } });

                await KubernetesHelper.CreateSecret(_kubernetes, secretName, @namespace, DefaultLabel,
                    new Dictionary<string, byte[]> { { "k3s.yaml", kubeRemoteBytes } });

                await KubernetesHelper.CreateExternalName(_kubernetes, externalName, @namespace, DefaultLabel, crd.Spec.Server);
                await CreateCodeServerDeployment(@namespace, codeServerApp.Image, codeServerSelector, pvcName, cmName, secretName, codeServerApp.Port);
                await CreateShellInABoxDeployment(@namespace, shellInABoxApp.Image, shellInABoxSelector, pvcName, secretName, crd.Spec.PortalPassword, shellInABoxApp.Port);
                await KubernetesHelper.CreateClusterIpService(_kubernetes, codeServerClusterIp, @namespace, DefaultLabel,
                    codeServerSelector, new Dictionary<int, int> { { 80, codeServerApp.Port } });
                await KubernetesHelper.CreateClusterIpService(_kubernetes, shellInABoxClusterIp, @namespace, DefaultLabel,
                    shellInABoxSelector, new Dictionary<int, int> { { 80, shellInABoxApp.Port } });

                await KubernetesHelper.CreateIngressRoute(_kubernetes, codeServerRoute, @namespace, DefaultLabel,
                    _crdIngressRoute, "web", $"Host(`{codeServerApp.Prefix}.{baseUrl}`)", 10,
                    new Dictionary<string, int> { { codeServerClusterIp, 80 } });
                await KubernetesHelper.CreateIngressRoute(_kubernetes, shellInABoxRoute, @namespace, DefaultLabel,
                    _crdIngressRoute, "web", $"Host(`{shellInABoxApp.Prefix}.{baseUrl}`)", 10,
                    new Dictionary<string, int> { { shellInABoxClusterIp, 80 } });
                await KubernetesHelper.CreateIngressRoute(_kubernetes, otherRoute, @namespace, DefaultLabel,
                    _crdIngressRoute, "web", "HostRegexp(`{subdomain:[a-z]+}." + baseUrl + "`)", 5,
                    new Dictionary<string, int> { { externalName, 80 } });
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error on create");
            }
        }

        public async Task OnDeleted(EventPortal crd)
        {
            try
            {
                if (await KubernetesHelper.NamespaceExists(_kubernetes, crd.Spec.Namespace, DefaultLabel))
                {
                    await KubernetesHelper.DeleteNamespace(_kubernetes, crd.Spec.Namespace);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "error on delete");
            }
        }

        public Task OnUpdated(EventPortal crd)
        {
            return Task.CompletedTask;
            
        }

        public Task OnError(EventPortal crd)
        {
            _logger.LogError($"error occur in event portal {crd.Metadata.Name}");
            return Task.CompletedTask;
        }

        public async Task CheckCurrentState(CustomResourceDefinition<EventPortal> crd, string @namespace)
        {
            try
            {
                var objects = await crd.GetObjectsAsync(_kubernetes, @namespace);
                _logger.LogInformation($"{objects.Count} items of {crd.Kind} exists");
                foreach (var obj in objects)
                {
                    _logger.LogInformation($"server: {obj.Spec.Server} - baseurl {obj.Spec.BaseUrl}");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"error on check current state {crd.Kind}");
            }
        }

        private async Task CreateCodeServerDeployment(string @namespace, string image, Dictionary<string, string> selector, string pvcName, string cmName, string secretName, int port)
        {
            var deploymentName = "code-server-deployment";

            var deployments = await _kubernetes.ListNamespacedDeploymentWithHttpMessagesAsync(@namespace);
            if (deployments.Body.Items.Any(x => KubernetesHelper.Match(x.Metadata, deploymentName, DefaultLabel)))
            {
                return;
            }

            var deployment = new V1Deployment
            {
                Metadata = new V1ObjectMeta
                {
                    Name = deploymentName,
                    Labels = DefaultLabel
                },
                Spec = new V1DeploymentSpec
                {
                    Selector = new V1LabelSelector { MatchLabels = selector },
                    Replicas = 1,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = selector
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = "app",
                                    Image = image,
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount
                                        {
                                            Name = "data", MountPath = "/home/coder/data"
                                        },
                                        new V1VolumeMount
                                        {
                                            Name = "auth", MountPath = "/home/coder/.config/code-server/config.yaml", SubPath = "config.yaml"
                                        },
                                        new V1VolumeMount
                                        {
                                            Name = "kube", MountPath = "/share/.kube/"
                                        }
                                    },
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = port
                                        }
                                    }
                                }
                            },
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume
                                {
                                    Name = "data",
                                    PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                                    {
                                        ClaimName = pvcName
                                    }
                                },
                                new V1Volume
                                {
                                    Name = "auth",
                                    ConfigMap = new V1ConfigMapVolumeSource
                                    {
                                        Name = cmName
                                    }
                                },
                                new V1Volume
                                {
                                    Name = "kube",
                                    Secret = new V1SecretVolumeSource
                                    {
                                        SecretName = secretName,
                                        Items = new List<V1KeyToPath>
                                        {
                                            new V1KeyToPath
                                            {
                                                Key = "k3s.yaml", Path = "config"
                                            }
                                        }
                                    }

                                },
                            }

                        }
                    }

                }
            };


            await _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(deployment, @namespace);

        }

        private async Task CreateShellInABoxDeployment(string @namespace, string image, Dictionary<string, string> selector, string pvcName, string secretName, string password, int port)
        {
            var deploymentName = "shell-in-a-box-deployment";

            var deployments = await _kubernetes.ListNamespacedDeploymentWithHttpMessagesAsync(@namespace);
            if (deployments.Body.Items.Any(x => KubernetesHelper.Match(x.Metadata, deploymentName, DefaultLabel)))
            {
                return;
            }

            var deployment = new V1Deployment
            {
                Metadata = new V1ObjectMeta
                {
                    Name = deploymentName,
                    Labels = DefaultLabel
                },
                Spec = new V1DeploymentSpec
                {
                    Selector = new V1LabelSelector { MatchLabels = selector },
                    Replicas = 1,
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = selector
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = "app",
                                    Image = image,
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount
                                        {
                                            Name = "data", MountPath = "/share/data"
                                        },
                                        new V1VolumeMount
                                        {
                                            Name = "kube", MountPath = "/share/.kube/"
                                        }
                                    },
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar
                                        {
                                            Name = "SIAB_SSL", Value = "0"
                                        },
                                        new V1EnvVar
                                        {
                                            Name = "SIAB_SUDO", Value = "true"
                                        },
                                        new V1EnvVar
                                        {
                                            Name = "SIAB_PASSWORD", Value = password
                                        },
                                    },
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort
                                        {
                                            ContainerPort = port
                                        }
                                    }
                                }
                            },
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume
                                {
                                    Name = "data",
                                    PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                                    {
                                        ClaimName = pvcName
                                    }
                                },
                                new V1Volume
                                {
                                    Name = "kube",
                                    Secret = new V1SecretVolumeSource
                                    {
                                        SecretName = secretName,
                                        Items = new List<V1KeyToPath>
                                        {
                                            new V1KeyToPath
                                            {
                                                Key = "k3s.yaml", Path = "config"
                                            }
                                        }
                                    }

                                },
                            }

                        }
                    }

                }
            };


            await _kubernetes.CreateNamespacedDeploymentWithHttpMessagesAsync(deployment, @namespace);

        }

    }
}
