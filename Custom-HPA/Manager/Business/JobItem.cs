namespace FP.ContainerTraining.Hpa.Manager.Business;

public class JobItem
{
    private string? _assignedWorker;

    public JobItem()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
    }
    public Guid Id { get; }
    
    public DateTime CreatedAt { get; }
    
    public DateTime?  AssignedAt { get; private set; }

    public string? AssignedWorker
    {
        get => _assignedWorker;
        set
        {
            _assignedWorker = value;
            AssignedAt = string.IsNullOrEmpty(value) ? null : DateTime.UtcNow;
        } 
    }

    public DateTime? CompletedAt { get; set; }
}