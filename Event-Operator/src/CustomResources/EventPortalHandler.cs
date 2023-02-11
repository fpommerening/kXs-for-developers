using System.Text;
using FP.ContainerTraining.EventOperator.Business;
using k8s;
using k8s.Models;

namespace FP.ContainerTraining.EventOperator.CustomResources;

public class EventPortalHandler : ICustomerResourceHandler<EventPortal>
{
    private Dictionary<string, string> DefaultLabel = new() { { "ControlledBy", "Community-Event-Operator" } };

    private readonly ILogger _logger;
    private readonly IKubernetes _kubernetes;
    private readonly CustomResourceDefinition<IngressRoute> _crdIngressRoute;

    public EventPortalHandler(ILogger<EventPortalHandler> logger, IKubernetes kubernetes, CustomResourceDefinition<IngressRoute> crdIngressRoute)
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
            var codeServercmName = "portal-code-server-cm";
            var nginxProxycmName = "portal-nginx-proxy-cm";
            var kubeSecretName = "portal-kube-secret";
            var sshSecretName = "portal-ssh-secret";
            var codeServerClusterIp = "portal-code-server-srv";
            var shellInABoxClusterIp = "portal-shell-in-a-box-srv";
            var nginxProxyClusterIp = "portal-nginx-proxy-srv";

            var codeServerRoute = "portal-code-server-ir";
            var shellInABoxRoute = "portal-shell-in-a-box-ir";
            var otherRoute = "portal-other-ir";

            var baseUrl = crd.Spec.BaseUrl;

            var codeServerSelector = new Dictionary<string, string>(DefaultLabel) { { "app", "code-server" } };
            var shellInABoxSelector = new Dictionary<string, string>(DefaultLabel) { { "app", "shell-in-a-box" } };
            var nginxProxySelector = new Dictionary<string, string>(DefaultLabel) { { "app", "nginx-proxy" } };

            var sbCodeServerConfig = new StringBuilder();
            sbCodeServerConfig.AppendLine("bind-addr: 127.0.0.1:8080");
            sbCodeServerConfig.AppendLine("auth: password");
            sbCodeServerConfig.AppendLine($"password: {crd.Spec.PortalPassword}");
            sbCodeServerConfig.AppendLine("cert: false");

            var sbNginxProxyConfig = new StringBuilder();
            sbNginxProxyConfig.AppendLine("user nginx;");
            sbNginxProxyConfig.AppendLine("worker_processes auto;");
            sbNginxProxyConfig.AppendLine("error_log /var/log/nginx/error.log notice;");
            sbNginxProxyConfig.AppendLine("pid /var/run/nginx.pid;");
            sbNginxProxyConfig.AppendLine("events {");
            sbNginxProxyConfig.AppendLine("  worker_connections 1024;");
            sbNginxProxyConfig.AppendLine("}");
            sbNginxProxyConfig.AppendLine("stream {");
            sbNginxProxyConfig.AppendLine(" upstream http_cluster {");
            sbNginxProxyConfig.AppendLine($"   server {crd.Spec.Server}:80;");
            sbNginxProxyConfig.AppendLine("}");
            sbNginxProxyConfig.AppendLine("server {");
            sbNginxProxyConfig.AppendLine("  listen 80;");
            sbNginxProxyConfig.AppendLine("  proxy_pass http_cluster;");
            sbNginxProxyConfig.AppendLine(" }");
            sbNginxProxyConfig.AppendLine("}");

            byte[] kubeLocalBytes = null;

            //var kubeLocalBytes = System.IO.File.ReadAllBytes(@"c:\temp\k3s.yaml");
            if (string.IsNullOrWhiteSpace(crd.Spec.SshPassword))
            {
                kubeLocalBytes = await SshHelper.GetRemoteFile(crd.Spec.Server, crd.Spec.SshUser, "/app/ssh/rsa_id",
                    string.Empty,
                    "/etc/rancher/k3s/k3s.yaml");
            }
            else
            {
                kubeLocalBytes = await SshHelper.GetRemoteFile(crd.Spec.Server, crd.Spec.SshUser, crd.Spec.SshPassword,
                    "/etc/rancher/k3s/k3s.yaml");
            }

            var kubeText = System.Text.Encoding.UTF8.GetString(kubeLocalBytes).Replace("127.0.0.1", crd.Spec.Server);
            var kubeRemoteBytes = System.Text.Encoding.UTF8.GetBytes(kubeText);

            await KubernetesHelper.CreateNamespace(_kubernetes, @namespace, DefaultLabel);
            await KubernetesHelper.CreatePersistentVolumeClaim(_kubernetes, pvcName, @namespace, DefaultLabel, 2048);
            await KubernetesHelper.CreateConfigMap(_kubernetes, codeServercmName, @namespace, DefaultLabel,
                new Dictionary<string, string> { { "config.yaml", sbCodeServerConfig.ToString() } });
            await KubernetesHelper.CreateConfigMap(_kubernetes, nginxProxycmName, @namespace, DefaultLabel,
                new Dictionary<string, string> { { "nginx.conf", sbNginxProxyConfig.ToString() } });
            await KubernetesHelper.CreateSecret(_kubernetes, kubeSecretName, @namespace, DefaultLabel,
                new Dictionary<string, byte[]> { { "k3s.yaml", kubeRemoteBytes } });
            await KubernetesHelper.CopySecret(_kubernetes, new Dictionary<string, string>(),
                crd.Metadata.NamespaceProperty, "workshop-ssh-secret", DefaultLabel,
                @namespace, sshSecretName);

            await CreateCodeServerDeployment(@namespace, codeServerApp.Image, codeServerSelector, pvcName,
                codeServercmName, kubeSecretName, codeServerApp.Port);
            await CreateShellInABoxDeployment(@namespace, shellInABoxApp.Image, shellInABoxSelector, pvcName,
                kubeSecretName, sshSecretName, crd.Spec.PortalPassword, shellInABoxApp.Port);
            await CreateNginxProxyDeployment(@namespace, nginxProxySelector, nginxProxycmName);

            await KubernetesHelper.CreateClusterIpService(_kubernetes, codeServerClusterIp, @namespace, DefaultLabel,
                codeServerSelector, new Dictionary<int, int> { { 80, codeServerApp.Port } });
            await KubernetesHelper.CreateClusterIpService(_kubernetes, shellInABoxClusterIp, @namespace, DefaultLabel,
                shellInABoxSelector, new Dictionary<int, int> { { 80, shellInABoxApp.Port } });
            await KubernetesHelper.CreateClusterIpService(_kubernetes, nginxProxyClusterIp, @namespace, DefaultLabel,
                nginxProxySelector, new Dictionary<int, int> { { 80, 80 } });

            await KubernetesHelper.CreateIngressRoute(_kubernetes, codeServerRoute, @namespace, DefaultLabel,
                _crdIngressRoute, "websecure", $"Host(`{codeServerApp.Prefix}.{baseUrl}`)", 10,
                new Dictionary<string, int> { { codeServerClusterIp, 80 } }, "traefik-external", "lets-encrypt-prod");
            await KubernetesHelper.CreateIngressRoute(_kubernetes, shellInABoxRoute, @namespace, DefaultLabel,
                _crdIngressRoute, "websecure", $"Host(`{shellInABoxApp.Prefix}.{baseUrl}`)", 10,
                new Dictionary<string, int> { { shellInABoxClusterIp, 80 } }, "traefik-external","lets-encrypt-prod");
            await KubernetesHelper.CreateIngressRoute(_kubernetes, otherRoute, @namespace, DefaultLabel,
                _crdIngressRoute, "web", "HostRegexp(`{subsubdomain:[a-z\\.]*}{subdomain:[a-z]+}." + baseUrl + "`)", 5,
                new Dictionary<string, int> { { nginxProxyClusterIp, 80 } },"traefik-external");
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
        _logger.LogError("error occur in event portal {Name}", crd.Metadata.Name);
        return Task.CompletedTask;
    }

    public async Task CheckCurrentState(CustomResourceDefinition<EventPortal> crd, string @namespace)
    {
        try
        {
            var objects = await crd.GetObjectsAsync(_kubernetes, @namespace);
            _logger.LogInformation("{ObjectsCount} items of {CrdKind} exists", objects.Count, crd.Kind);
            foreach (var obj in objects)
            {
                _logger.LogInformation("server: {Server} - baseurl {BaseUrl}", obj.Spec.Server, obj.Spec.BaseUrl);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "error on check current state {Kind}", crd.Kind);
        }
    }

    private async Task CreateCodeServerDeployment(string @namespace, string image, Dictionary<string, string> selector,
        string pvcName, string cmName, string secretName, int port)
    {
        var deploymentName = "code-server-deployment";

        var deployments = await _kubernetes.AppsV1.ListNamespacedDeploymentAsync(@namespace);
        if (deployments.Items.Any(x => KubernetesHelper.Match(x.Metadata, deploymentName, DefaultLabel)))
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
                                        Name = "auth", MountPath = "/home/coder/.config/code-server/config.yaml",
                                        SubPath = "config.yaml"
                                    },
                                    new V1VolumeMount
                                    {
                                        Name = "kube", MountPath = "/share/.kube/"
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


        await _kubernetes.AppsV1.CreateNamespacedDeploymentAsync(deployment, @namespace);

    }

    private async Task CreateShellInABoxDeployment(string @namespace, string image, Dictionary<string, string> selector,
        string pvcName, string kubeSecretName, string sshSecretName, string password, int port)
    {
        var deploymentName = "shell-in-a-box-deployment";

        var deployments = await _kubernetes.AppsV1.ListNamespacedDeploymentAsync(@namespace);
        if (deployments.Items.Any(x => KubernetesHelper.Match(x.Metadata, deploymentName, DefaultLabel)))
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
                                    },
                                    new V1VolumeMount
                                    {
                                        Name = "ssh", MountPath = "/share/.ssh/"
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
                                    SecretName = kubeSecretName,
                                    Items = new List<V1KeyToPath>
                                    {
                                        new V1KeyToPath
                                        {
                                            Key = "k3s.yaml", Path = "config"
                                        }
                                    }
                                }

                            },
                            new V1Volume
                            {
                                Name = "ssh",
                                Secret = new V1SecretVolumeSource
                                {
                                    SecretName = sshSecretName,
                                }
                            },
                        }

                    }
                }

            }
        };


        await _kubernetes.AppsV1.CreateNamespacedDeploymentAsync(deployment, @namespace);

    }


    private async Task CreateNginxProxyDeployment(string @namespace, Dictionary<string, string> selector, string cmName)
    {
        var deploymentName = "nginx-proxy-deployment";

        var deployments = await _kubernetes.AppsV1.ListNamespacedDeploymentAsync(@namespace);
        if (deployments.Items.Any(x => KubernetesHelper.Match(x.Metadata, deploymentName, DefaultLabel)))
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
                                Image = "nginx",
                                VolumeMounts = new List<V1VolumeMount>
                                {

                                    new V1VolumeMount
                                    {
                                        Name = "nginx-conf", MountPath = "/etc/nginx/nginx.conf", SubPath = "nginx.conf"
                                    },
                                },
                                Ports = new List<V1ContainerPort>
                                {
                                    new V1ContainerPort
                                    {
                                        ContainerPort = 80
                                    }
                                }
                            }
                        },
                        Volumes = new List<V1Volume>
                        {
                            new V1Volume
                            {
                                Name = "nginx-conf",
                                ConfigMap = new V1ConfigMapVolumeSource
                                {
                                    Name = cmName
                                }
                            },
                        }

                    }
                }

            }
        };
        await _kubernetes.AppsV1.CreateNamespacedDeploymentAsync(deployment, @namespace);
    }
}