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
            
            var unassignedJobs = _jobRepository.GetJobs()
                .Where(x => x.CompletedAt.HasValue == false)
                .Where(x => string.IsNullOrEmpty(x.AssignedWorker))
                .OrderBy(x=>x.CreatedAt)
                .ToArray();

            foreach (var job in unassignedJobs)
            {
                var targetWorker = await _workerRepository.AssignJob(job.Id);
                if (!string.IsNullOrEmpty(targetWorker))
                {
                    _jobRepository.Assign(job.Id, targetWorker);
                }
            }

            await Task.Delay(assignInterval, stoppingToken);
        }
        
    }
}