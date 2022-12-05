using FP.ContainerTraining.Hpa.Manager.Business;

namespace FP.ContainerTraining.Hpa.Manager.Services;

public class JobService : BackgroundService
{
    private readonly IJobRepository _jobRepository;
    private readonly IWorkerRepository _workerRepository;
    private readonly ILogger<JobService> _logger;

    public JobService(IJobRepository jobRepository, IWorkerRepository workerRepository, ILogger<JobService> logger)
    {
        _jobRepository = jobRepository;
        _workerRepository = workerRepository;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        var assignInterval = TimeSpan.FromSeconds(5);
        while (!stoppingToken.IsCancellationRequested)
        {
            var currentWorkers = _workerRepository.GetCurrentWorker();
            var assignedJobs = _jobRepository.GetJobs().Where(x => x.CompletedAt.HasValue == false)
                .Where(x => string.IsNullOrEmpty(x.AssignedWorker) == false)
                .ToList();

            foreach (var assignedJob in assignedJobs)
            {
                if (currentWorkers.All(cw => cw.HostName != assignedJob.AssignedWorker))
                {
                    _jobRepository.Reset(assignedJob.Id);
                }
            }
            
            var assignedWorkers = _jobRepository.GetJobs().Where(x => x.CompletedAt.HasValue == false)
                .Where(x => string.IsNullOrEmpty(x.AssignedWorker) == false)
                .Select(x=>x.AssignedWorker)
                .ToList();
            
            var unassignedJobs = _jobRepository.GetJobs()
                .Where(x => x.CompletedAt.HasValue == false)
                .Where(x => string.IsNullOrEmpty(x.AssignedWorker))
                .OrderBy(x=>x.CreatedAt)
                .ToArray();


            foreach (var unassignedJob in unassignedJobs)
            {
                
                _logger.LogInformation("assignedWorkers: {workers}", string.Join("|",assignedWorkers));
                _logger.LogInformation("currentWorkers: {workers}", string.Join("|",currentWorkers.Select(x=>x.HostName)));
                
                var unassignedWorker = currentWorkers.FirstOrDefault(cw => assignedWorkers.All(aw => aw != cw.HostName));
                
                _logger.LogInformation("unassignedWorker: {workers}", unassignedWorker?.HostName);
                while (unassignedWorker != null)
                {
                    _logger.LogInformation("Try assigned worker {WorkerName}", unassignedWorker.HostName);
                    if (await _workerRepository.AssignJob(unassignedWorker.HostName, unassignedJob.Id))
                    {
                        _jobRepository.Assign(unassignedJob.Id, unassignedWorker.HostName);
                        assignedWorkers.Add(unassignedWorker.HostName);
                        unassignedWorker = currentWorkers.FirstOrDefault(cw => assignedWorkers.All(aw => aw != cw.HostName));
                        _logger.LogInformation("unassignedWorker: {worker}", unassignedWorker?.HostName);
                        _logger.LogInformation("assignedWorkers: {workers}", string.Join("|",assignedWorkers));
                        _logger.LogInformation("currentWorkers: {workers}", string.Join("|",currentWorkers.Select(x=>x.HostName)));
                    }
                    else
                    {
                        _logger.LogWarning("Assigne Job {JobId} to worker {Worker} failed", unassignedJob.Id, unassignedWorker.HostName);
                    }
                }
            }

            await Task.Delay(assignInterval, stoppingToken);
        }
        
    }
}