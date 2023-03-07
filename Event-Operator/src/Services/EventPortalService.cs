using FP.ContainerTraining.EventOperator.CustomResources;

namespace FP.ContainerTraining.EventOperator.Services;

public class EventPortalService : BackgroundService
{
    private readonly Watcher<EventPortal> _watcher;

    public EventPortalService(Watcher<EventPortal> watcher)
    {
        _watcher = watcher;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _watcher.Ensure();
            await _watcher.CheckCurrentState();
            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }
}