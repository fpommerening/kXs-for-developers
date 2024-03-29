﻿using k8s;

namespace FP.ContainerTraining.EventOperator.CustomResources;

public class Watcher<T> where T : CustomResource
{
    private readonly IKubernetes _kubernetes;
    private readonly CustomResourceDefinition<T> _crd;
    private readonly ICustomerResourceHandler<T> _handler;
    private readonly ILogger<Watcher<T>> _logger;
    private k8s.Watcher<T>? _innerWatcher;
    private readonly string _namespace;

    public Watcher(IKubernetes kubernetes, CustomResourceDefinition<T> crd,
        ICustomerResourceHandler<T> handler, ILogger<Watcher<T>> logger, string @namespace)
    {
        _kubernetes = kubernetes;
        _crd = crd;
        _handler = handler;
        _logger = logger;
        _namespace = @namespace;
    }

    public void Ensure()
    {
        if (_innerWatcher is { Watching: false })
        {
            _innerWatcher.Dispose();
            _innerWatcher = null;
        }

        if (_innerWatcher != null)
        {
            return;
        }

        var listResponse = _kubernetes.CustomObjects.ListNamespacedCustomObjectWithHttpMessagesAsync(_crd.Group, _crd.Version,
                _namespace, _crd.Plural, watch: true);
        _innerWatcher = listResponse.Watch<T, object>(OnChange, OnError, OnClose);
    }

    public async Task CheckCurrentState()
    {
        await _handler.CheckCurrentState(_crd, _namespace);
    }

    private void OnClose()
    {
    }

    private void OnError(Exception ex)
    {
        _logger.LogError(ex, "Error on watching {CRD}", typeof(T).FullName);
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
                default:
                    _logger.LogWarning("Don't know what to do with {EventType}", eventType);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error on OnChange {EventType} - {ObjectName}", eventType, item.Metadata.Name);
        }
    }
}