using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FP.ContainerTraining.EventOperator.CustomResources;
using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FP.ContainerTraining.EventOperator.Services
{
    public class EventPortalService : BackgroundService
    {
        private readonly IConfiguration _configuration;
        private readonly Kubernetes _client;
        private readonly CustomResourceDefinition<EventPortal> _crd;
        private readonly EventPortalHandler _handler;

        public EventPortalService(Kubernetes client, CustomResourceDefinition<EventPortal> crd, EventPortalHandler handler, IConfiguration configuration)
        {
            _client = client;
            _crd = crd;
            _handler = handler;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _crd.Watch(_client, _handler, _configuration["PortalNamespace"]);

                await _handler.CheckCurrentState(_crd, _configuration["PortalNamespace"]);

                await Task.Delay(60000, stoppingToken);
            }
        }
    }
}
