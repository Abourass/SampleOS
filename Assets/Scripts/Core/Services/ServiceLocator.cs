using System;
using System.Collections.Generic;
using UnityEngine;

namespace SampleOS.Core.Services
{
  /// <summary>
  /// Central registry for game services. Thread-safe singleton.
  /// </summary>
  public class ServiceLocator
  {
    private static ServiceLocator instance;
    private static readonly object lockObject = new object();

    public static ServiceLocator Instance
    {
      get
      {
        if (instance == null)
        {
          lock (lockObject)
          {
            if (instance == null)
            {
              instance = new ServiceLocator();
            }
          }
        }
        return instance;
      }
    }

    private readonly Dictionary<Type, object> services = new Dictionary<Type, object>();
    private readonly Dictionary<Type, List<WeakReference>> serviceObservers = new Dictionary<Type, List<WeakReference>>();

    /// <summary>
    /// Register a service instance
    /// </summary>
    public void Register<T>(T service) where T : class
    {
      var type = typeof(T);

      if (services.ContainsKey(type))
      {
        Debug.LogWarning($"Service {type.Name} is already registered. Replacing...");
      }

      services[type] = service;
      NotifyServiceRegistered(type, service);

      Debug.Log($"Service registered: {type.Name}");
    }

    /// <summary>
    /// Get a registered service
    /// </summary>
    public T Get<T>() where T : class
    {
      var type = typeof(T);

      if (services.TryGetValue(type, out var service))
      {
        return service as T;
      }

      Debug.LogError($"Service {type.Name} not found!");
      return null;
    }

    /// <summary>
    /// Try to get a service without logging errors
    /// </summary>
    public bool TryGet<T>(out T service) where T : class
    {
      var type = typeof(T);

      if (services.TryGetValue(type, out var serviceObj))
      {
        service = serviceObj as T;
        return service != null;
      }

      service = null;
      return false;
    }

    /// <summary>
    /// Check if a service is registered
    /// </summary>
    public bool IsRegistered<T>() where T : class
    {
      return services.ContainsKey(typeof(T));
    }

    /// <summary>
    /// Unregister a service
    /// </summary>
    public void Unregister<T>() where T : class
    {
      var type = typeof(T);

      if (services.Remove(type))
      {
        Debug.Log($"Service unregistered: {type.Name}");
      }
    }

    /// <summary>
    /// Subscribe to service registration events
    /// </summary>
    public void OnServiceRegistered<T>(Action<T> callback) where T : class
    {
      var type = typeof(T);

      if (!serviceObservers.ContainsKey(type))
      {
        serviceObservers[type] = new List<WeakReference>();
      }

      serviceObservers[type].Add(new WeakReference(callback));
    }

    private void NotifyServiceRegistered(Type type, object service)
    {
      if (!serviceObservers.TryGetValue(type, out var observers))
        return;

      // Clean up dead references and notify alive ones
      for (int i = observers.Count - 1; i >= 0; i--)
      {
        if (!observers[i].IsAlive)
        {
          observers.RemoveAt(i);
        }
        else if (observers[i].Target is Delegate callback)
        {
          callback.DynamicInvoke(service);
        }
      }
    }

    /// <summary>
    /// Clear all services (for testing or scene transitions)
    /// </summary>
    public void Clear()
    {
      services.Clear();
      serviceObservers.Clear();
      Debug.Log("All services cleared");
    }
  }
}
