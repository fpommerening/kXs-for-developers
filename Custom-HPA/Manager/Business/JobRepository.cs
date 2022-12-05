namespace FP.ContainerTraining.Hpa.Manager.Business;

public class JobRepository : IJobRepository
{
    public JobRepository()
    {
        WorkerMetrics.Metrics.CreateObservableCounter("current_jobs",
            () => _jobs.Count(j => j.CompletedAt.HasValue == false));
    }
    
    private readonly List<JobItem> _jobs = new();

    public async Task Add(int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            _jobs.Add(new JobItem());
            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }
    }

    public void Assign(Guid id, string worker)
    {
        var job = _jobs.FirstOrDefault(x => x.Id == id);
        if (job != null)
        {
            job.AssignedWorker = worker;    
        }
    }
    
    public void Reset(Guid id)
    {
        var job = _jobs.FirstOrDefault(x => x.Id == id);
        if (job != null)
        {
            job.AssignedWorker = string.Empty;
        }
    }

    public void Complete(Guid id)
    {
        var job = _jobs.FirstOrDefault(x => x.Id == id);
        if (job != null)
        {
            job.CompletedAt = DateTime.UtcNow;
        }
    }

    public IReadOnlyList<JobItem> GetJobs()
    {
        return _jobs.AsReadOnly();
    }

}