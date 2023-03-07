using k8s;
using k8s.Models;

namespace FP.ContainerTraining.EventOperator.CustomResources;

public abstract class CustomResource : IKubernetesObject<V1ObjectMeta>
{
    public string Kind { get; set; } = string.Empty;

    public string ApiVersion { get; set; } = string.Empty;

    public V1ObjectMeta Metadata { get; set; } = null!;
}

public abstract class CustomResource<T> : CustomResource, ISpec<T> where T : class
{
    public T Spec { get; set; } = null!;
}