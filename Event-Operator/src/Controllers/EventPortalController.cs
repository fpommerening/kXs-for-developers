using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using FP.ContainerTraining.EventOperator.CustomResources;
using FP.ContainerTraining.EventOperator.Models;
using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FP.ContainerTraining.EventOperator.Controllers
{
    public class EventPortalController : Controller
    {
        private readonly ILogger<EventPortalController> _logger;
        private readonly IConfiguration _configuration;
        private readonly Kubernetes _client;
        private readonly CustomResourceDefinition<EventPortal> _crdEventPortal;
        private readonly CustomResourceDefinition<IngressRoute> _crdIngressRoute;

        public EventPortalController(ILogger<EventPortalController> logger, IConfiguration configuration, Kubernetes client, CustomResourceDefinition<EventPortal> crdEventPortal, CustomResourceDefinition<IngressRoute> crdIngressRoute)
        {
            _logger = logger;
            _configuration = configuration;
            _client = client;
            _crdEventPortal = crdEventPortal;
            _crdIngressRoute = crdIngressRoute;
        }

        public async Task<IActionResult> Index()
        {
            var objects = await _crdEventPortal.GetObjectsAsync(_client, _configuration["PortalNamespace"]);
            var models = objects.Select(x => new EventPortalModel
            {
                Name = x.Metadata.Name,
                CreationTimestamp = x.Metadata.CreationTimestamp,
                BaseUrl = x.Spec.BaseUrl,
                Namespace = x.Spec.Namespace,
                Server = x.Spec.Server,
            }).ToArray();
            return View(models);
        }

        public async Task<IActionResult> Detail(string name)
        {
            var objects = await _crdEventPortal.GetObjectsAsync(_client, _configuration["PortalNamespace"]);
            var obj = objects.FirstOrDefault(x => x.Metadata.Name == name);
            if (obj == null)
            {
                return NoContent();
            }

            var @namespace = obj.Spec.Namespace;

            var pods = await _client.ListNamespacedPodWithHttpMessagesAsync(@namespace);
            var deployments = await _client.ListNamespacedDeploymentWithHttpMessagesAsync(@namespace);
            var services = await _client.ListNamespacedServiceWithHttpMessagesAsync(@namespace);
            var ingressroutes = await _crdIngressRoute.GetObjectsAsync(_client, @namespace);


            var model = new EventPortalDetailModel()
            {
                Name = obj.Metadata.Name,
                CreationTimestamp = obj.Metadata.CreationTimestamp,
                BaseUrl = obj.Spec.BaseUrl,
                Namespace = obj.Spec.Namespace,
                Server = obj.Spec.Server,
                SshUser = obj.Spec.SshUser,
                SshPassword = obj.Spec.SshPassword,
                PortalPassword = obj.Spec.PortalPassword,
                CodeServer = MapEventPortalApp(obj.Spec.CodeServer),
                ShellInABox = MapEventPortalApp(obj.Spec.ShellInABox),
            };

            model.ObjectsInNamespace.AddRange(pods.Body.Items.Select(MapKubernetesObject));
            model.ObjectsInNamespace.AddRange(deployments.Body.Items.Select(MapKubernetesObject));
            model.ObjectsInNamespace.AddRange(services.Body.Items.Select(MapKubernetesObject));
            model.ObjectsInNamespace.AddRange(ingressroutes.Select(MapKubernetesObject));

            return View(model);
        }

     

        public async Task<IActionResult> RestartCodeServer(string name)
        {
            var objects = await _crdEventPortal.GetObjectsAsync(_client, _configuration["PortalNamespace"]);
            var obj = objects.FirstOrDefault(x => x.Metadata.Name == name);
            if (obj == null)
            {
                return NoContent();
            }

            var @namespace = obj.Spec.Namespace;

            var pods = await _client.ListNamespacedPodWithHttpMessagesAsync(obj.Spec.Namespace);
            var pod = pods.Body.Items.FirstOrDefault(x => x.Metadata.Name.StartsWith("code-server-deployment"));

            if (pod == null)
            {
                return BadRequest("Pod not found");
            }

            await _client.DeleteNamespacedPodWithHttpMessagesAsync(pod.Metadata.Name, @namespace);
            await Task.Delay(5000);
            return RedirectToAction("Detail", (object)name);
        }

        public async Task<IActionResult> RestartConsole(string name)
        {
            var objects = await _crdEventPortal.GetObjectsAsync(_client, _configuration["PortalNamespace"]);
            var obj = objects.FirstOrDefault(x => x.Metadata.Name == name);
            if (obj == null)
            {
                return NoContent();
            }

            var @namespace = obj.Spec.Namespace;

            var pods = await _client.ListNamespacedPodWithHttpMessagesAsync(obj.Spec.Namespace);
            var pod = pods.Body.Items.FirstOrDefault(x => x.Metadata.Name.StartsWith("shell-in-a-box-deployment"));

            if (pod == null)
            {
                return BadRequest("Pod not found");
            }

            await _client.DeleteNamespacedPodWithHttpMessagesAsync(pod.Metadata.Name, @namespace);
            await Task.Delay(5000);
            return RedirectToAction("Detail", (object)name);
        }

        private EventPortalAppModel MapEventPortalApp(EventPortalApplicationSpec app)
        {
            return new EventPortalAppModel
            {
                Image = app.Image,
                Port = app.Port,
                Prefix = app.Prefix
            };
        }

        private KubernetesMetaModel MapKubernetesObject(V1Pod pod)
        {
            return new KubernetesMetaModel
            {
                ApiVersion = "v1", // deployment.ApiVersion
                Kind = "Pod", // deployment.Kind,
                CreationTimestamp = pod.Metadata.CreationTimestamp,
                Name = pod.Metadata.Name
            };
        }

        private KubernetesMetaModel MapKubernetesObject(V1Deployment deployment)
        {
            return new KubernetesMetaModel
            {
                ApiVersion = "apps/v1", // deployment.ApiVersion
                Kind = "Deployment", // deployment.Kind,
                CreationTimestamp = deployment.Metadata.CreationTimestamp,
                Name = deployment.Metadata.Name
            };
        }

        private KubernetesMetaModel MapKubernetesObject(V1Service service)
        {
            return new KubernetesMetaModel
            {
                ApiVersion = "v1", // deployment.ApiVersion
                Kind = "Service", // deployment.Kind,
                CreationTimestamp = service.Metadata.CreationTimestamp,
                Name = service.Metadata.Name
            };
        }

        private KubernetesMetaModel MapKubernetesObject(IngressRoute ingressRoute)
        {
            return new KubernetesMetaModel
            {
                ApiVersion = ingressRoute.ApiVersion,
                Kind = ingressRoute.Kind,
                CreationTimestamp = ingressRoute.Metadata.CreationTimestamp,
                Name = ingressRoute.Metadata.Name
            };
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
