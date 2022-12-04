using System.Diagnostics.Metrics;

namespace FP.ContainerTraining.Hpa.Manager.Business;

public class WorkerMetrics
{
    internal static readonly Meter Metrics = new("WorkerMetrics");
    
}