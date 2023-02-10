namespace FP.ContainerTraining.EventOperator.CustomResources;

public interface ICustomerResourceHandler<T> where T : CustomResource
{
    Task OnAdded(T crd);

    Task OnDeleted(T crd);

    Task OnUpdated(T crd);

    Task OnError(T crd);

    Task CheckCurrentState(CustomResourceDefinition<T> crd, string @namespace);
}