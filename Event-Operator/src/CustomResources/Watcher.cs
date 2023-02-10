using k8s;
using k8s.Autorest;

namespace FP.ContainerTraining.EventOperator.CustomResources;

public class Watcher<T> where T : CustomResource
{
    private readonly IKubernetes _kubernetes;
    private readonly CustomResourceDefinition<T> _crd;
    private readonly ICustomerResourceHandler<T> _handler;
    private k8s.Watcher<T> _innerWatcher;
    private readonly string _namespace;

    public Watcher(IKubernetes kubernetes, CustomResourceDefinition<T> crd,
        ICustomerResourceHandler<T> handler, string @namespace)
    {
        _kubernetes = kubernetes;
        _crd = crd;
        _handler = handler;
        _namespace = @namespace;
    }

    public void Ensure()
    {
        Task<HttpOperationResponse<object>> listResponse = null;

        if (_innerWatcher is { Watching: false })
        {
            _innerWatcher.Dispose();
            _innerWatcher = null;
        }

        if (_innerWatcher != null)
        {
            return;
        }

        listResponse = _kubernetes.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(_crd.Group, _crd.Version,
                _namespace, _crd.Plural, watch: true);
        _innerWatcher = listResponse.Watch<T, object>(OnChange, OnError, OnClose);
    }

    private void OnClose()
    {
    }

    private void OnError(Exception obj)
    {
    }

    private async void OnChange(WatchEventType eventType, T item)
    {
        try
        {
            switch (eventType)
            {
                case WatchEventType.Added:
                    await _handler.OnAdded(item);
                    break;
                case WatchEventType.Modified:
                    await _handler.OnUpdated(item);
                    break;
                case WatchEventType.Deleted:
                    await _handler.OnDeleted(item);
                    break;
                case WatchEventType.Bookmark:

                    break;
                case WatchEventType.Error:
                    await _handler.OnError(item);
                    break;
                    return;
                default:
                    //Log.Warn($"Don't know what to do with {type}");
                    break;
            }

            ;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error on OnChange {eventType} - {item.Metadata.Name} \n {ex}");
        }
    }
}