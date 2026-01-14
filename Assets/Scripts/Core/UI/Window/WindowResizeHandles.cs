using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SampleOS.Core.UI.Window
{
    /// <summary>
    /// Manages resize handles for window edges and corners.
    /// Each handle detects drag events and reports resize deltas.
    /// </summary>
    public class WindowResizeHandles : MonoBehaviour
    {
        [Header("Edge Handles")]
        [SerializeField] private RectTransform topHandle;
        [SerializeField] private RectTransform bottomHandle;
        [SerializeField] private RectTransform leftHandle;
        [SerializeField] private RectTransform rightHandle;

        [Header("Corner Handles")]
        [SerializeField] private RectTransform topLeftHandle;
        [SerializeField] private RectTransform topRightHandle;
        [SerializeField] private RectTransform bottomLeftHandle;
        [SerializeField] private RectTransform bottomRightHandle;

        [Header("Style")]
        [SerializeField] private WindowChromeStyle style;

        /// <summary>
        /// Event fired when resize occurs. Parameters: direction, delta position.
        /// </summary>
        public event Action<ResizeDirection, Vector2> OnResize;

        /// <summary>
        /// Event fired when resize begins.
        /// </summary>
        public event Action OnResizeBegin;

        /// <summary>
        /// Event fired when resize ends.
        /// </summary>
        public event Action OnResizeEnd;

        private void Awake()
        {
            SetupHandle(topHandle, ResizeDirection.Top);
            SetupHandle(bottomHandle, ResizeDirection.Bottom);
            SetupHandle(leftHandle, ResizeDirection.Left);
            SetupHandle(rightHandle, ResizeDirection.Right);
            SetupHandle(topLeftHandle, ResizeDirection.TopLeft);
            SetupHandle(topRightHandle, ResizeDirection.TopRight);
            SetupHandle(bottomLeftHandle, ResizeDirection.BottomLeft);
            SetupHandle(bottomRightHandle, ResizeDirection.BottomRight);
        }

        private void SetupHandle(RectTransform handle, ResizeDirection direction)
        {
            if (handle == null) return;

            // Add or get the ResizeHandle component
            var resizeHandle = handle.gameObject.GetComponent<ResizeHandle>();
            if (resizeHandle == null)
            {
                resizeHandle = handle.gameObject.AddComponent<ResizeHandle>();
            }

            resizeHandle.Initialize(direction, this, style);
        }

        internal void NotifyResizeBegin()
        {
            OnResizeBegin?.Invoke();
        }

        internal void NotifyResize(ResizeDirection direction, Vector2 delta)
        {
            OnResize?.Invoke(direction, delta);
        }

        internal void NotifyResizeEnd()
        {
            OnResizeEnd?.Invoke();
        }

        /// <summary>
        /// Updates handle sizes based on style settings.
        /// </summary>
        public void ApplyStyle(WindowChromeStyle newStyle)
        {
            style = newStyle;
            if (style == null) return;

            float edgeSize = style.resizeHandleSize;
            float cornerSize = style.resizeCornerSize;

            // Update edge handle sizes
            UpdateEdgeHandle(topHandle, edgeSize, true);
            UpdateEdgeHandle(bottomHandle, edgeSize, true);
            UpdateEdgeHandle(leftHandle, edgeSize, false);
            UpdateEdgeHandle(rightHandle, edgeSize, false);

            // Update corner handle sizes
            UpdateCornerHandle(topLeftHandle, cornerSize);
            UpdateCornerHandle(topRightHandle, cornerSize);
            UpdateCornerHandle(bottomLeftHandle, cornerSize);
            UpdateCornerHandle(bottomRightHandle, cornerSize);
        }

        private void UpdateEdgeHandle(RectTransform handle, float size, bool horizontal)
        {
            if (handle == null) return;

            if (horizontal)
            {
                handle.sizeDelta = new Vector2(handle.sizeDelta.x, size);
            }
            else
            {
                handle.sizeDelta = new Vector2(size, handle.sizeDelta.y);
            }
        }

        private void UpdateCornerHandle(RectTransform handle, float size)
        {
            if (handle == null) return;
            handle.sizeDelta = new Vector2(size, size);
        }

        /// <summary>
        /// Enables or disables all resize handles.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            SetHandleActive(topHandle, enabled);
            SetHandleActive(bottomHandle, enabled);
            SetHandleActive(leftHandle, enabled);
            SetHandleActive(rightHandle, enabled);
            SetHandleActive(topLeftHandle, enabled);
            SetHandleActive(topRightHandle, enabled);
            SetHandleActive(bottomLeftHandle, enabled);
            SetHandleActive(bottomRightHandle, enabled);
        }

        private void SetHandleActive(RectTransform handle, bool active)
        {
            if (handle != null)
            {
                handle.gameObject.SetActive(active);
            }
        }
    }

    /// <summary>
    /// Direction of resize operation.
    /// </summary>
    public enum ResizeDirection
    {
        None,
        Top,
        Bottom,
        Left,
        Right,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// Component attached to individual resize handle GameObjects.
    /// Handles pointer events for resizing.
    /// </summary>
    public class ResizeHandle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private ResizeDirection direction;
        private WindowResizeHandles parent;
        private WindowChromeStyle style;
        private UnityEngine.UI.Image handleImage;
        private Color originalColor;

        public void Initialize(ResizeDirection dir, WindowResizeHandles parentHandles, WindowChromeStyle chromeStyle)
        {
            direction = dir;
            parent = parentHandles;
            style = chromeStyle;

            // Get or add image component for hover effect
            handleImage = GetComponent<UnityEngine.UI.Image>();
            if (handleImage != null)
            {
                originalColor = handleImage.color;
            }

            // Set appropriate cursor (would need custom cursor implementation)
            SetupCursor();
        }

        private void SetupCursor()
        {
            // Cursor changes would be implemented here
            // For now, we rely on visual feedback
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (handleImage != null && style != null)
            {
                handleImage.color = style.resizeHandleHoverColor;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (handleImage != null)
            {
                handleImage.color = originalColor;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            Debug.Log($"[ResizeHandle] Begin drag on {direction} handle");
            parent?.NotifyResizeBegin();
        }

        public void OnDrag(PointerEventData eventData)
        {
            Debug.Log($"[ResizeHandle] Dragging {direction} handle, delta: {eventData.delta}");
            parent?.NotifyResize(direction, eventData.delta);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Debug.Log($"[ResizeHandle] End drag on {direction} handle");
            parent?.NotifyResizeEnd();
        }
    }
}
