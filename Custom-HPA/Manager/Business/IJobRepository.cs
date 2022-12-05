namespace FP.ContainerTraining.Hpa.Manager.Business;

public interface IJobRepository
{
    Task Add(int count = 1);
    void Assign(Guid id, string worker);
    void Reset(Guid id);
    void Complete(Guid id);
    IReadOnlyList<JobItem> GetJobs();
}