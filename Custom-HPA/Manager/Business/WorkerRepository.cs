using System.Net.Http.Headers;
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

    public async Task<bool> AssignJob(string workerName, Guid jobId)
    {
        try
        {
            if(_workers.TryGetValue(workerName, out var worker))
            {
                _logger.LogInformation("Sending jobs to worker {Worker}", worker.HostName);
                await worker.CommandStream.WriteAsync(new GetCommandResponse
                {
                    Command = "DoJob",
                    Id = jobId.ToString("D")
                });
                _logger.LogInformation("Job to worker {Worker} succed", worker.HostName);
                return true;
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Job to worker {Worker} failed", workerName);
        }
        return false;
    }
}