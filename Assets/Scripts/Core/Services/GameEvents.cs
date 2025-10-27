using System;
using System.Collections.Generic;

namespace SampleOS.Core.Services
{
  public enum GameEventType
  {
    // Player events
    PlayerLocationChanged,
    PlayerDeviceSwitched,

    // Network events
    DeviceCompromised,
    NetworkAlertTriggered,
    NearbyDevicesChanged,
    NetworkChanged,

    // Device Events
    DeviceLocationChanged,
    DeviceFileSystemChanged,
    DeviceBackdoorInstalled,
    DeviceBackdoorRemoved,

    // World events
    TimeProgressed,
    DayChanged,

    // UI events
    TerminalOpened,
    TerminalClosed,
    AppFocused,

    // System events
    GameExiting
  }

  /// <summary>
  /// Global event bus for cross-layer communication
  /// </summary>
  public class GameEvents
  {
    private static GameEvents instance;
    public static GameEvents Instance => instance ??= new GameEvents();

    private Dictionary<GameEventType, List<Action<object>>> listeners =
        new Dictionary<GameEventType, List<Action<object>>>();

    public void Subscribe(GameEventType eventType, Action<object> callback)
    {
      if (!listeners.ContainsKey(eventType))
      {
        listeners[eventType] = new List<Action<object>>();
      }

      listeners[eventType].Add(callback);
    }

    public void Unsubscribe(GameEventType eventType, Action<object> callback)
    {
      if (listeners.ContainsKey(eventType))
      {
        listeners[eventType].Remove(callback);
      }
    }

    public void Trigger(GameEventType eventType, object data = null)
    {
      if (listeners.TryGetValue(eventType, out var callbacks))
      {
        foreach (var callback in callbacks.ToArray()) // ToArray to avoid modification issues
        {
          callback?.Invoke(data);
        }
      }
    }
  }
}
