using k8s;
using k8s.Models;

namespace FP.ContainerTraining.EventOperator.CustomResources;

public abstract class CustomResource : IKubernetesObject<V1ObjectMeta>
{
    public string Kind { get; set; }

    public string ApiVersion { get; set; }

    public V1ObjectMeta Metadata { get; set; }
}

public abstract class CustomResource<T> : CustomResource, ISpec<T> where T : class
{
    public T Spec { get; set; }

}