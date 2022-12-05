using FP.ContainerTraining.Hpa.Manager.Business;

namespace FP.ContainerTraining.Hpa.Manager.Services;

public class HeartbeatService : BackgroundService
{
    private readonly IWorkerRepository _workerRepository;
    private readonly IConfiguration _configuration;

    public HeartbeatService(IWorkerRepository workerRepository, IConfiguration configuration)
    {
        _workerRepository = workerRepository;
        _configuration = configuration;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

        var heartBeatIntervalSeconds = Convert.ToInt32(_configuration["HeartbeatIntervalSeconds"]);
        var heartBeatInterval = TimeSpan.FromSeconds(heartBeatIntervalSeconds);
        while (!stoppingToken.IsCancellationRequested)
        {
            await _workerRepository.SendHeartbeats();
            await Task.Delay(heartBeatInterval, stoppingToken);
        }
    }
}