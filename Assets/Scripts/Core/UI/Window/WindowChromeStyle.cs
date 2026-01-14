using UnityEngine;
using TMPro;

namespace SampleOS.Core.UI.Window
{
    /// <summary>
    /// ScriptableObject defining OS-specific visual styling for window chrome.
    /// Create different assets for Linux, Windows, and Mac aesthetics.
    /// </summary>
    [CreateAssetMenu(fileName = "WindowChromeStyle", menuName = "SampleOS/UI/Window Chrome Style")]
    public class WindowChromeStyle : ScriptableObject
    {
        [Header("Title Bar")]
        [Tooltip("Height of the title bar in pixels")]
        public float titleBarHeight = 32f;

        [Tooltip("Title bar background color when window is focused")]
        public Color titleBarFocusedColor = new Color(0.18f, 0.11f, 0.31f, 1f); // Deep purple

        [Tooltip("Title bar background color when window is unfocused")]
        public Color titleBarUnfocusedColor = new Color(0.15f, 0.15f, 0.18f, 1f); // Dark gray

        [Tooltip("Optional gradient end color (set alpha to 0 to disable gradient)")]
        public Color titleBarGradientEndColor = new Color(0.42f, 0.23f, 0.49f, 1f); // Pink-purple

        [Header("Title Text")]
        public Color titleTextColor = Color.white;
        public TMP_FontAsset titleFont;
        public float titleFontSize = 14f;
        public FontStyles titleFontStyle = FontStyles.Normal;

        [Header("Window Controls")]
        [Tooltip("Position of window control buttons")]
        public ControlButtonPosition controlPosition = ControlButtonPosition.Right;

        [Tooltip("Size of control buttons in pixels")]
        public float controlButtonSize = 24f;

        [Tooltip("Spacing between control buttons")]
        public float controlButtonSpacing = 4f;

        [Tooltip("Padding from edge of title bar")]
        public float controlButtonPadding = 8f;

        [Header("Close Button")]
        public Sprite closeButtonSprite;
        public Color closeButtonColor = new Color(1f, 0.4f, 0.4f, 1f); // Coral red
        public Color closeButtonHoverColor = new Color(1f, 0.2f, 0.2f, 1f); // Bright red

        [Header("Minimize Button")]
        public Sprite minimizeButtonSprite;
        public Color minimizeButtonColor = new Color(1f, 0.85f, 0.4f, 1f); // Yellow
        public Color minimizeButtonHoverColor = new Color(1f, 0.75f, 0.2f, 1f); // Bright yellow

        [Header("Maximize Button")]
        public Sprite maximizeButtonSprite;
        public Sprite restoreButtonSprite;
        public Color maximizeButtonColor = new Color(0.4f, 0.9f, 0.5f, 1f); // Green
        public Color maximizeButtonHoverColor = new Color(0.3f, 1f, 0.4f, 1f); // Bright green

        [Header("Border")]
        [Tooltip("Width of window border in pixels")]
        public float borderWidth = 2f;

        public Color borderFocusedColor = new Color(0.5f, 0.3f, 0.7f, 1f); // Purple
        public Color borderUnfocusedColor = new Color(0.3f, 0.3f, 0.35f, 1f); // Gray

        [Tooltip("9-slice sprite for border (optional)")]
        public Sprite borderSprite;

        [Tooltip("Corner radius for rounded borders")]
        public float borderCornerRadius = 8f;

        [Header("Shadow")]
        public bool enableShadow = true;
        public Color shadowColor = new Color(0f, 0f, 0f, 0.3f);
        public Vector2 shadowOffset = new Vector2(4f, -4f);
        public float shadowBlur = 8f;

        [Header("Resize Handles")]
        [Tooltip("Size of edge resize handles")]
        public float resizeHandleSize = 6f;

        [Tooltip("Size of corner resize handles")]
        public float resizeCornerSize = 12f;

        [Tooltip("Color when hovering over resize handle")]
        public Color resizeHandleHoverColor = new Color(0.6f, 0.4f, 0.8f, 0.5f);

        [Header("Animation")]
        public float focusAnimationDuration = 0.15f;
        public float minimizeAnimationDuration = 0.2f;
        public float maximizeAnimationDuration = 0.2f;

        [Header("Content Area")]
        [Tooltip("Padding inside the window for content area")]
        public float contentPadding = 2f;

        public Color contentBackgroundColor = new Color(0.1f, 0.1f, 0.12f, 0.95f);
    }

    public enum ControlButtonPosition
    {
        Left,   // macOS style
        Right   // Windows style
    }
}
