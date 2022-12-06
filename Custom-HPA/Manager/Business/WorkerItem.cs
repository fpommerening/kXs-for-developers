using FP.ContainerTraining.Hpa.Contract;
using Grpc.Core;

namespace FP.ContainerTraining.Hpa.Manager.Business;

public class WorkerItem
{
    public string HostName { get; set; }
        
    public DateTime CreatedAt { get; set; }
        
    public DateTime LastHeartbeat { get; set; }
    
    public IServerStreamWriter<GetCommandResponse> CommandStream { get; set; }
    
    public CancellationTokenSource CancellationTokenSource { get; set; }

    public Guid AssignedJobId { get; set; } = Guid.Empty;

}