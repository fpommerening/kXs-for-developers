using FP.ContainerTraining.Hpa.Manager.Business;

namespace FP.ContainerTraining.Hpa.Manager.Services;

public class JobService : BackgroundService
{
    private readonly IJobRepository _jobRepository;
    private readonly IWorkerRepository _workerRepository;

    public JobService(IJobRepository jobRepository, IWorkerRepository workerRepository)
    {
        _jobRepository = jobRepository;
        _workerRepository = workerRepository;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        var assignInterval = TimeSpan.FromSeconds(5);
        while (!stoppingToken.IsCancellationRequested)
        {
            var currentWorker = _workerRepository.GetCurrentWorker();
            var assignedJobs = _jobRepository.GetJobs().Where(x => x.CompletedAt.HasValue == false)
                .Where(x => string.IsNullOrEmpty(x.AssignedWorker) == false)
                .ToList();

            foreach (var assignedJob in assignedJobs)
            {
                if (currentWorker.All(cw => cw.HostName != assignedJob.AssignedWorker))
                {
                    _jobRepository.Reset(assignedJob.Id);
                }
            }
            
            var assignedWorker = _jobRepository.GetJobs().Where(x => x.CompletedAt.HasValue == false)
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
                var unassignedWorker = currentWorker.FirstOrDefault(cw => assignedWorker.All(aw => aw != cw.HostName));
                while (unassignedWorker != null)
                {
                    if (await _workerRepository.AssignJob(unassignedWorker.HostName, unassignedJob.Id))
                    {
                        _jobRepository.Assign(unassignedJob.Id, unassignedWorker.HostName);
                        assignedWorker.Add(unassignedWorker.HostName);
                        unassignedWorker = currentWorker.FirstOrDefault(cw => assignedWorker.All(aw => aw != cw.HostName));
                    }
                }
            }

            await Task.Delay(assignInterval, stoppingToken);
        }
        
    }
}