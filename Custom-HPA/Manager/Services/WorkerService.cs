using FP.ContainerTraining.Hpa.Contract;
using FP.ContainerTraining.Hpa.Manager.Business;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace FP.ContainerTraining.Hpa.Manager.Services;

public class WorkerService : WorkerServices.WorkerServicesBase
{
    private readonly ILogger<WorkerService> _logger;
    private readonly IWorkerRepository _workerRepository;

    public WorkerService(ILogger<WorkerService> logger, IWorkerRepository workerRepository)
    {
        _logger = logger;
        _workerRepository = workerRepository;
    }

    public override async Task GetCommand(GetCommandRequest request, IServerStreamWriter<GetCommandResponse> responseStream, ServerCallContext context)
    {
        var hostname = request.Instance;
        try
        {
            _workerRepository.Add(hostname, responseStream);

            await Task.Delay(-1, context.CancellationToken);
        }
        catch (Exception e)
        {
            _workerRepository.Remove(hostname);
        }
    }

    public override Task<Empty> SendResult(SendResultRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Recevice Result form Task {TaskId}", request.Id);
        return Task.FromResult(new Empty());
    }
}