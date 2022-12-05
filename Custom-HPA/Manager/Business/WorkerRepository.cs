using FP.ContainerTraining.Hpa.Contract;
using Grpc.Core;

namespace FP.ContainerTraining.Hpa.Manager.Business;

public class WorkerRepository : IWorkerRepository
{
    private readonly ILogger<WorkerRepository> _logger;
    private readonly Dictionary<string, WorkerItem> _workers = new();
    private readonly int _heartBeatIntervalSeconds;

    public WorkerRepository(ILogger<WorkerRepository> logger, IConfiguration configuration)
    {
        _logger = logger;
        WorkerMetrics.Metrics.CreateObservableCounter("current_worker", () => _workers.Count);
        _heartBeatIntervalSeconds = Convert.ToInt32(configuration["HeartbeatIntervalSeconds"]);
    }

    public async Task Add(string hostName, IServerStreamWriter<GetCommandResponse> commandStream)
    {
        if (!_workers.ContainsKey(hostName))
        {
            var cancellationTokenSource = new CancellationTokenSource();
            _workers[hostName] = new WorkerItem
            {
                CreatedAt = DateTime.UtcNow,
                HostName = hostName,
                LastHeartbeat = DateTime.UtcNow,
                CommandStream = commandStream,
                CancellationTokenSource = cancellationTokenSource
            };
            await Task.Delay(-1, cancellationTokenSource.Token);
        }
    }

    public void Remove(string hostName)
    {
        if(_workers.TryGetValue(hostName, out var worker))
        {
            _logger.LogInformation("Remove worker {Worker}", worker.HostName);
            worker.CancellationTokenSource.Cancel();
            _workers.Remove(hostName);
        }
    }

    public async Task SendHeartbeats()
    {
        var tasks = _workers.Values.Select(SendHeartbeat);
        await Task.WhenAll(tasks);
    }

    private async Task SendHeartbeat(WorkerItem worker)
    {
        try
        {
            _logger.LogInformation("Sending heartbeat to worker {Worker}", worker.HostName);
            
                await worker.CommandStream.WriteAsync(new GetCommandResponse
                {
                    Command = "Heartbeat",
                    Id = Guid.NewGuid().ToString("D")
                });
                worker.LastHeartbeat = DateTime.UtcNow;
                
                _logger.LogInformation("Heartbeat to worker {Worker} succed", worker.HostName);

        }
        catch (Exception e)
        {
            _logger.LogError(e, "Heartbeat to worker {Worker} failed", worker.HostName);
            if (worker.LastHeartbeat.AddSeconds(_heartBeatIntervalSeconds * 3) < DateTime.UtcNow)
            {
                Remove(worker.HostName);
            }
        }
    }

    public IReadOnlyList<WorkerItem> GetCurrentWorker()
    {
        return _workers.Values.ToArray();
    }
}

public class WorkerItem
{
    public string HostName { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public DateTime LastHeartbeat { get; set; }
    
    public IServerStreamWriter<GetCommandResponse> CommandStream { get; set; }
    
    public CancellationTokenSource CancellationTokenSource { get; set; }
    
}

