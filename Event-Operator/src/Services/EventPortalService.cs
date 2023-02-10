using FP.ContainerTraining.EventOperator.CustomResources;
using k8s;

namespace FP.ContainerTraining.EventOperator.Services;

public class EventPortalService : BackgroundService
{
    private readonly IKubernetes _kubernetes;
    private readonly CustomResourceDefinition<EventPortal> _crd;
    private readonly EventPortalHandler _handler;
    private readonly IConfiguration _configuration;

    public EventPortalService(IKubernetes kubernetes, CustomResourceDefinition<EventPortal> crd, EventPortalHandler handler, IConfiguration configuration)
    {
        _kubernetes = kubernetes;
        _crd = crd;
        _handler = handler;
        _configuration = configuration;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var portalNamespace = _configuration["PortalNamespace"]!;
        while (!stoppingToken.IsCancellationRequested)
        {
            _crd.Watch(_kubernetes, _handler, portalNamespace);
            await _handler.CheckCurrentState(_crd, portalNamespace);
            await Task.Delay(60000, stoppingToken);
        }
    }
}