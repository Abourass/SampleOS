using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using SampleOS.Core.Services;

namespace SampleOS.Core.UI.Window
{
    /// <summary>
    /// Main window chrome component that handles dragging, resizing, and window controls.
    /// Attach this to the root of a window prefab.
    /// </summary>
    public class WindowChrome : MonoBehaviour, IDragHandler, IBeginDragHandler, IEndDragHandler, IPointerDownHandler
    {
        [Header("Window References")]
        [SerializeField] private RectTransform windowRect;
        [SerializeField] private RectTransform titleBar;
        [SerializeField] private RectTransform contentArea;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Window Controls")]
        [SerializeField] private Button closeButton;
        [SerializeField] private Button minimizeButton;
        [SerializeField] private Button maximizeButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private Image titleBarBackground;
        [SerializeField] private Image borderImage;

        [Header("Resize")]
        [SerializeField] private WindowResizeHandles resizeHandles;

        [Header("Style")]
        [SerializeField] private WindowChromeStyle style;

        [Header("Constraints")]
        [SerializeField] private Vector2 minSize = new Vector2(300, 200);
        [SerializeField] private Vector2 maxSize = new Vector2(1920, 1080);

        // Public properties
        public string InstanceId { get; set; }
        public WindowState State { get; private set; } = WindowState.Normal;
        public RectTransform WindowRect => windowRect;

        // Events for ApplicationContainerService to wire up
        public event Action OnCloseRequested;
        public event Action OnMinimizeRequested;
        public event Action OnMaximizeRequested;
        public event Action OnFocusRequested;
        public event Action<Vector2> OnWindowMoved;
        public event Action<Vector2> OnWindowResized;

        // Private state
        private bool isDragging;
        private bool isResizing;
        private bool isFocused;
        private Rect normalBounds;
        private RectTransform parentRect;
        private Canvas parentCanvas;

        private void Awake()
        {
            if (windowRect == null)
                windowRect = GetComponent<RectTransform>();

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // Store initial bounds
            normalBounds = new Rect(windowRect.anchoredPosition, windowRect.sizeDelta);

            // Setup button listeners
            SetupButtons();

            // Setup resize handles
            if (resizeHandles != null)
            {
                resizeHandles.OnResize += HandleResize;
                resizeHandles.OnResizeBegin += () => isResizing = true;
                resizeHandles.OnResizeEnd += () => isResizing = false;
            }

            // Find parent canvas for bounds checking
            parentCanvas = GetComponentInParent<Canvas>();
            parentRect = transform.parent as RectTransform;
        }

        private void Start()
        {
            ApplyStyle();
        }

        private void OnDestroy()
        {
            if (resizeHandles != null)
            {
                resizeHandles.OnResize -= HandleResize;
            }
        }

        private void SetupButtons()
        {
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() => OnCloseRequested?.Invoke());
            }

            if (minimizeButton != null)
            {
                minimizeButton.onClick.AddListener(() => OnMinimizeRequested?.Invoke());
            }

            if (maximizeButton != null)
            {
                maximizeButton.onClick.AddListener(() =>
                {
                    if (State == WindowState.Maximized)
                    {
                        // Already maximized, this will restore
                        OnMaximizeRequested?.Invoke();
                    }
                    else
                    {
                        OnMaximizeRequested?.Invoke();
                    }
                });
            }
        }

        #region Title Bar Dragging

        public void OnPointerDown(PointerEventData eventData)
        {
            // Request focus when clicking anywhere on the window
            OnFocusRequested?.Invoke();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            // Only allow dragging from title bar
            if (!IsPointerOverTitleBar(eventData))
                return;

            // Don't allow dragging when maximized
            if (State == WindowState.Maximized)
                return;

            isDragging = true;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging)
                return;

            // Move window by drag delta
            Vector2 newPosition = windowRect.anchoredPosition + eventData.delta / GetCanvasScale();

            // Clamp to parent bounds (keep title bar on screen)
            newPosition = ClampToParentBounds(newPosition);

            windowRect.anchoredPosition = newPosition;
            OnWindowMoved?.Invoke(newPosition);
            GameEvents.Instance.Trigger(GameEventType.WindowMoved, InstanceId);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
        }

        private bool IsPointerOverTitleBar(PointerEventData eventData)
        {
            if (titleBar == null)
                return true; // If no title bar specified, allow dragging anywhere

            return RectTransformUtility.RectangleContainsScreenPoint(
                titleBar, eventData.position, eventData.pressEventCamera);
        }

        private float GetCanvasScale()
        {
            if (parentCanvas != null)
                return parentCanvas.scaleFactor;
            return 1f;
        }

        private Vector2 ClampToParentBounds(Vector2 position)
        {
            if (parentRect == null)
                return position;

            // Keep at least the title bar height visible on screen
            float titleBarHeight = style != null ? style.titleBarHeight : 32f;
            float halfWidth = windowRect.sizeDelta.x * 0.5f;
            float halfHeight = windowRect.sizeDelta.y * 0.5f;

            Vector2 parentSize = parentRect.rect.size;
            float minX = -parentSize.x * 0.5f + halfWidth - windowRect.sizeDelta.x + 100; // Keep 100px visible
            float maxX = parentSize.x * 0.5f - halfWidth + windowRect.sizeDelta.x - 100;
            float minY = -parentSize.y * 0.5f + halfHeight - windowRect.sizeDelta.y + titleBarHeight;
            float maxY = parentSize.y * 0.5f - halfHeight;

            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);

            return position;
        }

        #endregion

        #region Resizing

        private void HandleResize(ResizeDirection direction, Vector2 delta)
        {
            if (State == WindowState.Maximized)
                return;

            delta /= GetCanvasScale();

            Vector2 newSize = windowRect.sizeDelta;
            Vector2 newPos = windowRect.anchoredPosition;

            switch (direction)
            {
                case ResizeDirection.Right:
                    newSize.x += delta.x;
                    break;
                case ResizeDirection.Left:
                    newSize.x -= delta.x;
                    newPos.x += delta.x * 0.5f;
                    break;
                case ResizeDirection.Top:
                    newSize.y += delta.y;
                    newPos.y += delta.y * 0.5f;
                    break;
                case ResizeDirection.Bottom:
                    newSize.y -= delta.y;
                    newPos.y += delta.y * 0.5f;
                    break;
                case ResizeDirection.TopRight:
                    newSize.x += delta.x;
                    newSize.y += delta.y;
                    newPos.y += delta.y * 0.5f;
                    break;
                case ResizeDirection.TopLeft:
                    newSize.x -= delta.x;
                    newSize.y += delta.y;
                    newPos.x += delta.x * 0.5f;
                    newPos.y += delta.y * 0.5f;
                    break;
                case ResizeDirection.BottomRight:
                    newSize.x += delta.x;
                    newSize.y -= delta.y;
                    newPos.y += delta.y * 0.5f;
                    break;
                case ResizeDirection.BottomLeft:
                    newSize.x -= delta.x;
                    newSize.y -= delta.y;
                    newPos.x += delta.x * 0.5f;
                    newPos.y += delta.y * 0.5f;
                    break;
            }

            // Clamp size
            newSize.x = Mathf.Clamp(newSize.x, minSize.x, maxSize.x);
            newSize.y = Mathf.Clamp(newSize.y, minSize.y, maxSize.y);

            windowRect.sizeDelta = newSize;
            windowRect.anchoredPosition = newPos;

            OnWindowResized?.Invoke(newSize);
            GameEvents.Instance.Trigger(GameEventType.WindowResized, InstanceId);
        }

        #endregion

        #region Window State Management

        /// <summary>
        /// Sets the window title text.
        /// </summary>
        public void SetTitle(string title)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }
        }

        /// <summary>
        /// Sets the focused state of the window (visual feedback).
        /// </summary>
        public void SetFocused(bool focused)
        {
            isFocused = focused;
            UpdateFocusedVisuals();
        }

        /// <summary>
        /// Called by WindowManager when window should minimize.
        /// </summary>
        public void OnMinimize()
        {
            State = WindowState.Minimized;

            // Fade out the window
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }

            // Disable resize handles
            resizeHandles?.SetEnabled(false);
        }

        /// <summary>
        /// Called by WindowManager when window should maximize.
        /// </summary>
        public void OnMaximize()
        {
            // Store current bounds for restore
            if (State == WindowState.Normal)
            {
                normalBounds = new Rect(windowRect.anchoredPosition, windowRect.sizeDelta);
            }

            State = WindowState.Maximized;

            // Fill parent container
            if (parentRect != null)
            {
                windowRect.anchoredPosition = Vector2.zero;
                windowRect.sizeDelta = parentRect.rect.size;
            }

            // Update maximize button icon
            UpdateMaximizeButtonIcon();

            // Disable resize handles when maximized
            resizeHandles?.SetEnabled(false);
        }

        /// <summary>
        /// Called by WindowManager when window should restore.
        /// </summary>
        public void OnRestore(Rect bounds)
        {
            State = WindowState.Normal;

            // Restore visibility
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }

            // Restore size and position
            windowRect.anchoredPosition = bounds.position;
            windowRect.sizeDelta = bounds.size;

            // Update maximize button icon
            UpdateMaximizeButtonIcon();

            // Enable resize handles
            resizeHandles?.SetEnabled(true);
        }

        private void UpdateMaximizeButtonIcon()
        {
            if (maximizeButton == null || style == null)
                return;

            var image = maximizeButton.GetComponent<Image>();
            if (image != null)
            {
                image.sprite = State == WindowState.Maximized
                    ? style.restoreButtonSprite
                    : style.maximizeButtonSprite;
            }
        }

        #endregion

        #region Styling

        /// <summary>
        /// Applies the current style to all visual elements.
        /// </summary>
        public void ApplyStyle()
        {
            if (style == null)
                return;

            // Title bar
            if (titleBarBackground != null)
            {
                titleBarBackground.color = isFocused
                    ? style.titleBarFocusedColor
                    : style.titleBarUnfocusedColor;
            }

            if (titleBar != null)
            {
                titleBar.sizeDelta = new Vector2(titleBar.sizeDelta.x, style.titleBarHeight);
            }

            // Title text
            if (titleText != null)
            {
                titleText.color = style.titleTextColor;
                titleText.fontSize = style.titleFontSize;
                if (style.titleFont != null)
                    titleText.font = style.titleFont;
                titleText.fontStyle = style.titleFontStyle;
            }

            // Border
            if (borderImage != null)
            {
                borderImage.color = isFocused
                    ? style.borderFocusedColor
                    : style.borderUnfocusedColor;

                if (style.borderSprite != null)
                    borderImage.sprite = style.borderSprite;
            }

            // Window control buttons
            ApplyButtonStyle(closeButton, style.closeButtonColor, style.closeButtonSprite);
            ApplyButtonStyle(minimizeButton, style.minimizeButtonColor, style.minimizeButtonSprite);
            ApplyButtonStyle(maximizeButton, style.maximizeButtonColor,
                State == WindowState.Maximized ? style.restoreButtonSprite : style.maximizeButtonSprite);

            // Resize handles
            resizeHandles?.ApplyStyle(style);

            // Position controls based on style
            PositionWindowControls();
        }

        private void ApplyButtonStyle(Button button, Color color, Sprite sprite)
        {
            if (button == null)
                return;

            var image = button.GetComponent<Image>();
            if (image != null)
            {
                image.color = color;
                if (sprite != null)
                    image.sprite = sprite;
            }

            // Set button size
            var rect = button.GetComponent<RectTransform>();
            if (rect != null && style != null)
            {
                rect.sizeDelta = new Vector2(style.controlButtonSize, style.controlButtonSize);
            }
        }

        private void PositionWindowControls()
        {
            if (style == null || closeButton == null)
                return;

            // Get the controls container (parent of buttons)
            Transform controlsContainer = closeButton.transform.parent;
            if (controlsContainer == null)
                return;

            var containerRect = controlsContainer.GetComponent<RectTransform>();
            if (containerRect == null)
                return;

            // Position based on style setting
            if (style.controlPosition == ControlButtonPosition.Left)
            {
                containerRect.anchorMin = new Vector2(0, 0.5f);
                containerRect.anchorMax = new Vector2(0, 0.5f);
                containerRect.pivot = new Vector2(0, 0.5f);
                containerRect.anchoredPosition = new Vector2(style.controlButtonPadding, 0);
            }
            else // Right
            {
                containerRect.anchorMin = new Vector2(1, 0.5f);
                containerRect.anchorMax = new Vector2(1, 0.5f);
                containerRect.pivot = new Vector2(1, 0.5f);
                containerRect.anchoredPosition = new Vector2(-style.controlButtonPadding, 0);
            }
        }

        private void UpdateFocusedVisuals()
        {
            if (style == null)
                return;

            // Update title bar color
            if (titleBarBackground != null)
            {
                Color targetColor = isFocused
                    ? style.titleBarFocusedColor
                    : style.titleBarUnfocusedColor;

                // Could animate this transition
                titleBarBackground.color = targetColor;
            }

            // Update border color
            if (borderImage != null)
            {
                borderImage.color = isFocused
                    ? style.borderFocusedColor
                    : style.borderUnfocusedColor;
            }
        }

        #endregion

        #region Double-Click Maximize Toggle

        private float lastClickTime;
        private const float doubleClickThreshold = 0.3f;

        /// <summary>
        /// Check for double-click on title bar to toggle maximize.
        /// Call this from a pointer click handler on the title bar.
        /// </summary>
        public void OnTitleBarClick()
        {
            float currentTime = Time.unscaledTime;
            if (currentTime - lastClickTime < doubleClickThreshold)
            {
                // Double click - toggle maximize
                OnMaximizeRequested?.Invoke();
                lastClickTime = 0;
            }
            else
            {
                lastClickTime = currentTime;
            }
        }

        #endregion
    }
}
