using SampleOS.Core.Apps;

namespace SampleOS.Core.UI
{
    /// <summary>
    /// Interface for all interactive app UI views.
    /// Views are MonoBehaviour components that handle rendering and input,
    /// while business logic lives in IInteractiveApp implementations.
    /// </summary>
    public interface IAppView
    {
        /// <summary>
        /// Bind this view to an app instance
        /// </summary>
        void BindToApp(IInteractiveApp app);
        
        /// <summary>
        /// Unbind from the current app
        /// </summary>
        void UnbindFromApp();
        
        /// <summary>
        /// Show this view (make it visible)
        /// </summary>
        void Show();
        
        /// <summary>
        /// Hide this view (make it invisible but don't destroy)
        /// </summary>
        void Hide();
        
        /// <summary>
        /// Refresh the view to match current app state
        /// </summary>
        void RefreshView();
        
        /// <summary>
        /// Check if this view is currently bound to an app
        /// </summary>
        bool IsBound { get; }
        
        /// <summary>
        /// Get the app this view is bound to
        /// </summary>
        IInteractiveApp BoundApp { get; }
    }
}
