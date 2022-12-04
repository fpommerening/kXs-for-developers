using FP.ContainerTraining.Hpa.Contract;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace FP.ContainerTraining.Hpa.Manager.Services;

public class WorkerService : WorkerServices.WorkerServicesBase
{
    private readonly ILogger<WorkerService> _logger;

    public WorkerService(ILogger<WorkerService> logger)
    {
        _logger = logger;
    }

    public override Task GetCommand(GetCommandRequest request, IServerStreamWriter<GetCommandResponse> responseStream, ServerCallContext context)
    {
        return base.GetCommand(request, responseStream, context);
    }

    public override Task<Empty> SendResult(SendResultRequest request, ServerCallContext context)
    {
        _logger.LogInformation("Recevice Result form Task {TaskId}", request.Id);
        return Task.FromResult(new Empty());
    }
}