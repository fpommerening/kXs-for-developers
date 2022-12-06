using FP.ContainerTraining.Hpa.Contract;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FP.ContainerTraining.Hpa.Worker.Services;

public class WorkerService : BackgroundService
{
    private readonly ILogger<WorkerService> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _instance;

    public WorkerService(ILogger<WorkerService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
        _instance = Environment.MachineName;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var channel = GrpcChannel.ForAddress(_configuration["ManagerUrl"]);
        await Task.Delay(30_000, stoppingToken);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var client = new WorkerServices.WorkerServicesClient(channel);
                var getCommandRequest = new GetCommandRequest
                {
                    Instance = _instance,
                    Timestamp = DateTime.UtcNow.ToTimestamp()
                };
                using var streamingCall = client.GetCommand(getCommandRequest, cancellationToken: stoppingToken);
                await foreach (var command in streamingCall.ResponseStream.ReadAllAsync(
                                   cancellationToken: stoppingToken))
                {
                    if (command.Command.ToLowerInvariant() != "heartbeat")
                    {
                        ExecuteCommand(command.Id, client);
                    }
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Error on recive commands");
            }
            finally
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }

    private void ExecuteCommand(string id, WorkerServices.WorkerServicesClient client)
    {
        _logger.LogInformation("Executing Task {TaskId}", id);
        
        Task.Run(async () =>
        {
            await Task.Delay(TimeSpan.FromSeconds(45));
            await client.SendResultAsync(new SendResultRequest { Id = id, Instance = _instance });
            _logger.LogInformation("Executed Task {TaskId}", id);
        });

    }
}