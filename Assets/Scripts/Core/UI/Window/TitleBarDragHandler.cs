using UnityEngine;
using UnityEngine.EventSystems;
using SampleOS.Core.Services;

namespace SampleOS.Core.UI.Window
{
    /// <summary>
    /// Attach to the TitleBar to enable window dragging.
    /// Forwards drag events to the parent WindowChrome.
    /// </summary>
    public class TitleBarDragHandler : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
    {
        private WindowChrome windowChrome;
        private RectTransform windowRect;
        private Canvas parentCanvas;
        private bool isDragging;

        private void Awake()
        {
            // Find the WindowChrome in parent hierarchy
            windowChrome = GetComponentInParent<WindowChrome>();
            if (windowChrome != null)
            {
                windowRect = windowChrome.WindowRect;
            }
            parentCanvas = GetComponentInParent<Canvas>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            // Forward focus request to WindowChrome
            windowChrome?.RequestFocus();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (windowChrome == null || windowRect == null)
                return;

            // Don't allow dragging when maximized
            if (windowChrome.State == WindowState.Maximized)
                return;

            isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || windowRect == null)
                return;

            // Move window by drag delta
            float scale = parentCanvas != null ? parentCanvas.scaleFactor : 1f;
            Vector2 delta = eventData.delta / scale;
            windowRect.anchoredPosition += delta;

            windowChrome?.NotifyWindowMoved(windowRect.anchoredPosition);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }
    }
}
