using System.Diagnostics;
using System.Threading.Tasks;
using FP.ContainerTraining.EventOperator.CustomResources;
using FP.ContainerTraining.EventOperator.Models;
using k8s;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace FP.ContainerTraining.EventOperator.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _configuration;
        private readonly Kubernetes _client;
        private readonly CustomResourceDefinition<EventPortal> _crdEventPortal;

        public HomeController(ILogger<HomeController> logger, IConfiguration configuration, Kubernetes client, CustomResourceDefinition<EventPortal> crdEventPortal)
        {
            _logger = logger;
            _configuration = configuration;
            _client = client;
            _crdEventPortal = crdEventPortal;
        }

        public async Task<IActionResult> Index()
        {
            var objects = await _crdEventPortal.GetObjectsAsync(_client, _configuration["PortalNamespace"]);


            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
