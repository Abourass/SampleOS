using UnityEngine;
using SampleOS.Core.Services;

namespace SampleOS.Core.Apps
{
    /// <summary>
    /// MonoBehaviour responsible for updating all running apps each frame.
    /// Should be attached to a persistent GameObject in your scene.
    /// </summary>
    public class AppUpdater : MonoBehaviour
    {
        private IApplicationContainerService appContainer;
        
        [Header("Settings")]
        [Tooltip("Should this object persist across scene loads?")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake()
        {
            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            // Get the application container service
            if (ServiceLocator.Instance != null && 
                ServiceLocator.Instance.IsRegistered<IApplicationContainerService>())
            {
                appContainer = ServiceLocator.Instance.Get<IApplicationContainerService>();
                Debug.Log("[AppUpdater] Connected to ApplicationContainerService");
            }
            else
            {
                Debug.LogError("[AppUpdater] ApplicationContainerService not found! Make sure it's registered in ServiceLocator before this script starts.");
            }
        }

        private void Update()
        {
            if (appContainer != null)
            {
                appContainer.UpdateApps(Time.deltaTime);
            }
        }

        private void OnDestroy()
        {
            appContainer = null;
        }
    }
}
