using UnityEngine;
using SampleOS.Core.World;
using SampleOS.Core.Devices;
using SampleOS.Core.Session;
using SampleOS.Core.Networking;
using SampleOS.Core.Apps;

namespace SampleOS.Core.Services
{
  /// <summary>
  /// Central game state initialization and management.
  /// Bootstraps all services in correct order.
  /// </summary>
  public class GameStateManager : MonoBehaviour
  {
    private static GameStateManager instance;
    public static GameStateManager Instance => instance;

    [Header("Startup Configuration")]
    [SerializeField] private string startingCityId = "metropolis";
    [SerializeField] private bool loadFromSave = false;

    // Track if there are unsaved changes
    private bool hasUnsavedChanges = false;
    private System.DateTime? lastSaveTime = null;

    private void Awake()
    {
      if (instance != null && instance != this)
      {
        Destroy(gameObject);
        return;
      }

      instance = this;
      DontDestroyOnLoad(gameObject);

      InitializeGameState();
      SubscribeToProgressEvents();
    }

    private void InitializeGameState()
    {
      Debug.Log("=== Initializing Game State ===");

      // 0. Initialize Device Registry (needed by World and Network services)
      var deviceRegistry = new DeviceRegistry();
      ServiceLocator.Instance.Register<IDeviceRegistry>(deviceRegistry);
      deviceRegistry.Initialize();

      // 1. Initialize Vulnerability Database (needed by other services)
      var vulnDbService = new VulnerabilityDatabaseService();
      ServiceLocator.Instance.Register<IVulnerabilityDatabaseService>(vulnDbService);
      vulnDbService.Initialize();

      // 2. Initialize World (Cities, Networks, NPCs)
      var worldService = new WorldService();
      ServiceLocator.Instance.Register<IWorldService>(worldService);
      worldService.Initialize(startingCityId);

      // 3. Initialize Player State (Owned devices, inventory, progress)
      var playerService = new PlayerStateService();
      ServiceLocator.Instance.Register<IPlayerStateService>(playerService);

      // Get player's starting device from world
      var playerDevice = worldService.GetPlayerDevice();
      playerService.Initialize(playerDevice);

      // 5. Initialize Network Simulation (Device states, security updates)
      var networkService = new NetworkService();
      ServiceLocator.Instance.Register<INetworkService>(networkService);
      networkService.Initialize(worldService.GetAllNetworks());

      // 6. Initialize Application Container (App lifecycle management)
      var appContainerService = new ApplicationContainerService();
      ServiceLocator.Instance.Register<IApplicationContainerService>(appContainerService);
      appContainerService.Initialize();

      // 7. Initialize Window Manager (Window z-order, focus, state)
      var windowManager = new WindowManager();
      ServiceLocator.Instance.Register<IWindowManager>(windowManager);

      // 8. Setup cross-layer event subscriptions
      SetupEventBridge();

      Debug.Log("=== Game State Initialized ===");
    }

    /// <summary>
    /// Wire up events between layers
    /// </summary>
    private void SetupEventBridge()
    {
      var worldService = ServiceLocator.Instance.Get<IWorldService>();
      var networkService = ServiceLocator.Instance.Get<INetworkService>();

      // Example: When player physically enters a building, scan for nearby devices
      worldService.OnPlayerLocationChanged += (location) =>
      {
        var nearbyDevices = networkService.GetDevicesAtLocation(location);
        GameEvents.Instance.Trigger(GameEventType.NearbyDevicesChanged, nearbyDevices);
      };

      // Time progression affects network security
      worldService.OnTimeProgressed += (deltaTime) =>
      {
        networkService.UpdateSecurityStates(deltaTime);
      };
    }

    private void Update()
    {
      // Each service updates at its own rate
      ServiceLocator.Instance.Get<IWorldService>()?.Update(Time.deltaTime);
      ServiceLocator.Instance.Get<INetworkService>()?.Update(Time.deltaTime);
    }

    public void SaveGameState()
    {
      // Serialize all services
      var saveData = new GameSaveData
      {
        worldState = ServiceLocator.Instance.Get<IWorldService>().GetSaveData(),
        playerState = ServiceLocator.Instance.Get<IPlayerStateService>().GetSaveData(),
        networkState = ServiceLocator.Instance.Get<INetworkService>().GetSaveData(),
      };

      // Save to disk
      SaveSystem.Save(saveData);

      // Mark as saved
      hasUnsavedChanges = false;
      lastSaveTime = System.DateTime.Now;

      Debug.Log("Game saved successfully");
    }

    public void LoadGameState()
    {
      var saveData = SaveSystem.Load();

      ServiceLocator.Instance.Get<IWorldService>().LoadFromSave(saveData.worldState);
      if (saveData.playerState is PlayerSaveData playerData)
      {
        ServiceLocator.Instance.Get<IPlayerStateService>().LoadFromSave(playerData);
      }
      ServiceLocator.Instance.Get<INetworkService>().LoadFromSave(saveData.networkState);
    }

    /// <summary>
    /// Subscribe to events that indicate progress has been made
    /// </summary>
    private void SubscribeToProgressEvents()
    {
      var playerState = ServiceLocator.Instance.Get<IPlayerStateService>();

      if (playerState != null)
      {
        playerState.OnSystemCompromised += _ => MarkProgressMade();
        playerState.OnDeviceAcquired += _ => MarkProgressMade();
        playerState.Progress.OnSystemCompromised += _ => MarkProgressMade();
      }

      // Subscribe to game events
      GameEvents.Instance.Subscribe(GameEventType.DeviceCompromised, _ => MarkProgressMade());
    }

    private void MarkProgressMade()
    {
      hasUnsavedChanges = true;
    }

    /// <summary>
    /// Check if there are unsaved changes
    /// </summary>
    public bool HasUnsavedProgress()
    {
      if (!hasUnsavedChanges)
        return false;

      // Also check if any actual progress has been made
      var playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
      if (playerState == null)
        return false;

      // Check various progress indicators
      bool hasProgress =
          playerState.Progress.GetOwnedSystems().Count > 0 ||
          playerState.Progress.GetAllVulnerabilities().Count > 0 ||
          playerState.Credentials.GetTotalCredentialCount() > 0;

      return hasProgress;
    }

    /// <summary>
    /// Get time since last save
    /// </summary>
    public System.TimeSpan? GetTimeSinceLastSave()
    {
      if (lastSaveTime.HasValue)
      {
        return System.DateTime.Now - lastSaveTime.Value;
      }
      return null;
    }

    private void OnApplicationQuit()
    {
      // Auto-save on quit if there are unsaved changes
      if (hasUnsavedChanges)
      {
        Debug.Log("Auto-saving on quit...");
        SaveGameState();
      }
    }
  }
}
