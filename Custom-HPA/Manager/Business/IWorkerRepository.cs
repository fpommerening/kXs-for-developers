using FP.ContainerTraining.Hpa.Contract;
using Grpc.Core;

namespace FP.ContainerTraining.Hpa.Manager.Business;

public interface IWorkerRepository
{
    Task Add(string hostName, IServerStreamWriter<GetCommandResponse> commandStream);
    void Remove(string hostName);

    Task SendHeartbeats();

    IReadOnlyList<WorkerItem> GetCurrentWorker();

    Task<string> AssignJob(Guid jobId);
    
    void ResetJob(string hostName);
}