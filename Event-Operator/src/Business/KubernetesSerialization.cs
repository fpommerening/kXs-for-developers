using System.Text.Json;
namespace FP.ContainerTraining.EventOperator.Business;

public class KubernetesSerialization
{
    public static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
}