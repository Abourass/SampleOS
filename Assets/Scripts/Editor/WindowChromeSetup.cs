using UnityEngine;
using UnityEditor;
using SampleOS.Core.UI.Window;
using TMPro;
using UnityEngine.UI;

namespace SampleOS.Editor
{
    /// <summary>
    /// Editor utility for setting up Window Chrome assets and prefabs.
    /// </summary>
    public static class WindowChromeSetup
    {
        private const string StylesPath = "Assets/Resources/Configs/WindowStyles";

        [MenuItem("SampleOS/Window Chrome/Create All Style Assets")]
        public static void CreateAllStyleAssets()
        {
            CreateLinuxStyle();
            CreateWindowsStyle();
            CreateMacStyle();
            AssetDatabase.SaveAssets();
            Debug.Log("[WindowChromeSetup] Created all window style assets");
        }

        [MenuItem("SampleOS/Window Chrome/Create Linux Style")]
        public static void CreateLinuxStyle()
        {
            var style = ScriptableObject.CreateInstance<WindowChromeStyle>();

            // Linux: Deep purple/magenta gradient, circular controls on LEFT
            style.titleBarHeight = 32f;
            style.titleBarFocusedColor = new Color(0.35f, 0.15f, 0.45f, 1f);    // Deep purple
            style.titleBarUnfocusedColor = new Color(0.2f, 0.1f, 0.25f, 1f);    // Darker purple
            style.titleTextColor = new Color(0.95f, 0.8f, 1f, 1f);              // Light pink-lavender
            style.titleFontSize = 14f;
            style.titleFontStyle = FontStyles.Normal;

            // Control buttons - circular, LEFT side, order: close, minimize, maximize
            style.controlPosition = ControlButtonPosition.Left;
            style.controlButtonSize = 14f;
            style.controlButtonSpacing = 8f;
            style.controlButtonPadding = 12f;

            style.closeButtonColor = new Color(1f, 0.4f, 0.5f, 1f);             // Coral pink
            style.minimizeButtonColor = new Color(1f, 0.75f, 0.3f, 1f);         // Orange-yellow
            style.maximizeButtonColor = new Color(0.5f, 0.9f, 0.6f, 1f);        // Mint green

            // Border - neon purple glow
            style.borderWidth = 2f;
            style.borderFocusedColor = new Color(0.7f, 0.3f, 0.9f, 0.9f);       // Bright purple
            style.borderUnfocusedColor = new Color(0.4f, 0.2f, 0.5f, 0.6f);     // Dim purple

            // Resize handles
            style.resizeHandleSize = 6f;
            style.resizeHandleHoverColor = new Color(0.8f, 0.4f, 1f, 0.7f);

            style.focusAnimationDuration = 0.15f;

            SaveStyleAsset(style, "LinuxWindowStyle");
        }

        [MenuItem("SampleOS/Window Chrome/Create Windows Style")]
        public static void CreateWindowsStyle()
        {
            var style = ScriptableObject.CreateInstance<WindowChromeStyle>();

            // Windows: Blue gradient, square controls on RIGHT
            style.titleBarHeight = 32f;
            style.titleBarFocusedColor = new Color(0.2f, 0.4f, 0.6f, 1f);       // Steel blue
            style.titleBarUnfocusedColor = new Color(0.25f, 0.25f, 0.3f, 1f);   // Dark gray-blue
            style.titleTextColor = Color.white;
            style.titleFontSize = 13f;
            style.titleFontStyle = FontStyles.Normal;

            // Control buttons - square, RIGHT side, order: minimize, maximize, close
            style.controlPosition = ControlButtonPosition.Right;
            style.controlButtonSize = 32f;  // Taller buttons for Windows
            style.controlButtonSpacing = 1f;
            style.controlButtonPadding = 2f;

            style.closeButtonColor = new Color(0.9f, 0.2f, 0.2f, 1f);           // Red
            style.minimizeButtonColor = new Color(0.3f, 0.3f, 0.35f, 1f);       // Dark gray
            style.maximizeButtonColor = new Color(0.3f, 0.3f, 0.35f, 1f);       // Dark gray

            // Border - subtle blue
            style.borderWidth = 1f;
            style.borderFocusedColor = new Color(0.3f, 0.5f, 0.7f, 1f);
            style.borderUnfocusedColor = new Color(0.3f, 0.3f, 0.35f, 0.8f);

            // Resize handles
            style.resizeHandleSize = 5f;
            style.resizeHandleHoverColor = new Color(0.3f, 0.5f, 0.7f, 0.5f);

            style.focusAnimationDuration = 0.1f;

            SaveStyleAsset(style, "WindowsWindowStyle");
        }

        [MenuItem("SampleOS/Window Chrome/Create Mac Style")]
        public static void CreateMacStyle()
        {
            var style = ScriptableObject.CreateInstance<WindowChromeStyle>();

            // Mac: Light frosted glass, traffic light controls on LEFT
            style.titleBarHeight = 28f;
            style.titleBarFocusedColor = new Color(0.92f, 0.92f, 0.94f, 0.98f); // Light gray
            style.titleBarUnfocusedColor = new Color(0.85f, 0.85f, 0.87f, 0.95f);
            style.titleTextColor = new Color(0.15f, 0.15f, 0.15f, 1f);          // Dark text
            style.titleFontSize = 13f;
            style.titleFontStyle = FontStyles.Normal;

            // Control buttons - circular traffic lights, LEFT side, order: close, minimize, maximize
            style.controlPosition = ControlButtonPosition.Left;
            style.controlButtonSize = 12f;
            style.controlButtonSpacing = 8f;
            style.controlButtonPadding = 10f;

            style.closeButtonColor = new Color(1f, 0.38f, 0.34f, 1f);           // macOS red
            style.minimizeButtonColor = new Color(1f, 0.75f, 0.25f, 1f);        // macOS yellow
            style.maximizeButtonColor = new Color(0.2f, 0.78f, 0.35f, 1f);      // macOS green

            // Border - very subtle
            style.borderWidth = 1f;
            style.borderFocusedColor = new Color(0.6f, 0.6f, 0.65f, 0.4f);
            style.borderUnfocusedColor = new Color(0.5f, 0.5f, 0.55f, 0.3f);

            // Resize handles
            style.resizeHandleSize = 4f;
            style.resizeHandleHoverColor = new Color(0.4f, 0.4f, 0.45f, 0.4f);

            style.focusAnimationDuration = 0.2f;

            SaveStyleAsset(style, "MacWindowStyle");
        }

        private static void SaveStyleAsset(WindowChromeStyle style, string name)
        {
            if (!AssetDatabase.IsValidFolder(StylesPath))
            {
                string[] folders = StylesPath.Split('/');
                string currentPath = folders[0];
                for (int i = 1; i < folders.Length; i++)
                {
                    string nextPath = currentPath + "/" + folders[i];
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = nextPath;
                }
            }

            string assetPath = $"{StylesPath}/{name}.asset";
            AssetDatabase.CreateAsset(style, assetPath);
            Debug.Log($"[WindowChromeSetup] Created style asset: {assetPath}");
        }

        [MenuItem("SampleOS/Window Chrome/Add Chrome to Selected Prefab (Linux)")]
        public static void AddChromeLinux()
        {
            AddChromeToSelected(OSType.Linux);
        }

        [MenuItem("SampleOS/Window Chrome/Add Chrome to Selected Prefab (Windows)")]
        public static void AddChromeWindows()
        {
            AddChromeToSelected(OSType.Windows);
        }

        [MenuItem("SampleOS/Window Chrome/Add Chrome to Selected Prefab (Mac)")]
        public static void AddChromeMac()
        {
            AddChromeToSelected(OSType.Mac);
        }

        private enum OSType { Linux, Windows, Mac }

        private static void AddChromeToSelected(OSType osType)
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("[WindowChromeSetup] No GameObject selected.");
                return;
            }

            if (selected.GetComponent<WindowChrome>() != null)
            {
                Debug.LogWarning("[WindowChromeSetup] Selected object already has WindowChrome.");
                return;
            }

            AddWindowChromeHierarchy(selected, osType);
            Debug.Log($"[WindowChromeSetup] Added {osType} window chrome to {selected.name}");
        }

        private static void AddWindowChromeHierarchy(GameObject root, OSType osType)
        {
            RectTransform rootRect = root.GetComponent<RectTransform>();
            if (rootRect == null)
                rootRect = root.AddComponent<RectTransform>();

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = root.AddComponent<CanvasGroup>();

            WindowChrome chrome = root.AddComponent<WindowChrome>();

            // Create Border Frame
            GameObject borderFrame = CreateBorderFrame(root.transform, osType);

            // Create TitleBar with drag handler
            GameObject titleBar = CreateUIElement("TitleBar", root.transform);
            titleBar.transform.SetSiblingIndex(1);
            Image titleBarBg = titleBar.AddComponent<Image>();
            titleBarBg.color = GetTitleBarColor(osType);

            // Add the drag handler to enable window dragging
            titleBar.AddComponent<TitleBarDragHandler>();

            RectTransform titleBarRect = titleBar.GetComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 1);
            titleBarRect.anchorMax = new Vector2(1, 1);
            titleBarRect.pivot = new Vector2(0.5f, 1);
            titleBarRect.sizeDelta = new Vector2(0, GetTitleBarHeight(osType));
            titleBarRect.anchoredPosition = Vector2.zero;

            // Create Title Text
            GameObject titleTextObj = CreateUIElement("TitleText", titleBar.transform);
            TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Window Title";
            titleText.fontSize = osType == OSType.Linux ? 14 : 13;
            titleText.color = GetTitleTextColor(osType);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
            titleText.raycastTarget = false;
            SetRectTransformStretch(titleTextObj.GetComponent<RectTransform>());

            // Create Window Controls - order depends on OS
            Button closeBtn, minBtn, maxBtn;
            CreateWindowControls(titleBar.transform, osType, out closeBtn, out minBtn, out maxBtn);

            // Create ContentArea
            GameObject contentArea = CreateUIElement("ContentArea", root.transform);
            contentArea.transform.SetSiblingIndex(2);
            RectTransform contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(2, 2);
            contentRect.offsetMax = new Vector2(-2, -GetTitleBarHeight(osType) - 2);

            MoveExistingContentToArea(root.transform, contentArea.transform);

            // Create ResizeHandles
            GameObject resizeHandlesObj = CreateUIElement("ResizeHandles", root.transform);
            WindowResizeHandles handles = resizeHandlesObj.AddComponent<WindowResizeHandles>();
            SetRectTransformStretch(resizeHandlesObj.GetComponent<RectTransform>());
            CreateResizeHandles(resizeHandlesObj, handles);

            // Get border image reference
            Image borderImage = borderFrame.transform.Find("TopBorder")?.GetComponent<Image>();

            // Wire up serialized references
            SerializedObject serializedChrome = new SerializedObject(chrome);
            serializedChrome.FindProperty("windowRect").objectReferenceValue = rootRect;
            serializedChrome.FindProperty("titleBar").objectReferenceValue = titleBarRect;
            serializedChrome.FindProperty("contentArea").objectReferenceValue = contentRect;
            serializedChrome.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serializedChrome.FindProperty("closeButton").objectReferenceValue = closeBtn;
            serializedChrome.FindProperty("minimizeButton").objectReferenceValue = minBtn;
            serializedChrome.FindProperty("maximizeButton").objectReferenceValue = maxBtn;
            serializedChrome.FindProperty("titleText").objectReferenceValue = titleText;
            serializedChrome.FindProperty("titleBarBackground").objectReferenceValue = titleBarBg;
            if (borderImage != null)
                serializedChrome.FindProperty("borderImage").objectReferenceValue = borderImage;
            serializedChrome.FindProperty("resizeHandles").objectReferenceValue = handles;
            serializedChrome.ApplyModifiedProperties();

            Debug.Log("[WindowChromeSetup] Window chrome hierarchy created successfully");
        }

        private static void CreateWindowControls(Transform titleBar, OSType osType,
            out Button closeBtn, out Button minBtn, out Button maxBtn)
        {
            GameObject controls = CreateUIElement("WindowControls", titleBar);
            RectTransform controlsRect = controls.GetComponent<RectTransform>();

            bool isRightSide = (osType == OSType.Windows);
            float buttonSize = GetButtonSize(osType);
            float spacing = osType == OSType.Windows ? 1f : 8f;
            float padding = osType == OSType.Windows ? 2f : 12f;

            if (isRightSide)
            {
                controlsRect.anchorMin = new Vector2(1, 0.5f);
                controlsRect.anchorMax = new Vector2(1, 0.5f);
                controlsRect.pivot = new Vector2(1, 0.5f);
                controlsRect.anchoredPosition = new Vector2(-padding, 0);
            }
            else
            {
                controlsRect.anchorMin = new Vector2(0, 0.5f);
                controlsRect.anchorMax = new Vector2(0, 0.5f);
                controlsRect.pivot = new Vector2(0, 0.5f);
                controlsRect.anchoredPosition = new Vector2(padding, 0);
            }

            controlsRect.sizeDelta = new Vector2(buttonSize * 3 + spacing * 2 + 10, buttonSize);

            HorizontalLayoutGroup layout = controls.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = spacing;
            layout.childAlignment = isRightSide ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Create buttons in correct order based on OS
            if (osType == OSType.Windows)
            {
                // Windows: minimize, maximize, close (left to right when on right side)
                GameObject minBtnObj = CreateWindowButton("MinimizeButton", controls.transform, osType, ButtonType.Minimize);
                GameObject maxBtnObj = CreateWindowButton("MaximizeButton", controls.transform, osType, ButtonType.Maximize);
                GameObject closeBtnObj = CreateWindowButton("CloseButton", controls.transform, osType, ButtonType.Close);

                closeBtn = closeBtnObj.GetComponent<Button>();
                minBtn = minBtnObj.GetComponent<Button>();
                maxBtn = maxBtnObj.GetComponent<Button>();
            }
            else
            {
                // Mac/Linux: close, minimize, maximize (left to right on left side)
                GameObject closeBtnObj = CreateWindowButton("CloseButton", controls.transform, osType, ButtonType.Close);
                GameObject minBtnObj = CreateWindowButton("MinimizeButton", controls.transform, osType, ButtonType.Minimize);
                GameObject maxBtnObj = CreateWindowButton("MaximizeButton", controls.transform, osType, ButtonType.Maximize);

                closeBtn = closeBtnObj.GetComponent<Button>();
                minBtn = minBtnObj.GetComponent<Button>();
                maxBtn = maxBtnObj.GetComponent<Button>();
            }
        }

        private enum ButtonType { Close, Minimize, Maximize }

        private static GameObject CreateWindowButton(string name, Transform parent, OSType osType, ButtonType buttonType)
        {
            GameObject btn = CreateUIElement(name, parent);
            Image img = btn.AddComponent<Image>();
            img.color = GetButtonColor(osType, buttonType);
            btn.AddComponent<Button>();

            float size = GetButtonSize(osType);
            RectTransform rect = btn.GetComponent<RectTransform>();

            if (osType == OSType.Windows)
            {
                // Windows buttons are wider than tall
                rect.sizeDelta = new Vector2(46, size);
            }
            else
            {
                // Mac/Linux buttons are circular
                rect.sizeDelta = new Vector2(size, size);
            }

            // Add symbol
            string symbol = GetButtonSymbol(osType, buttonType);
            GameObject symbolObj = CreateUIElement("Symbol", btn.transform);
            TextMeshProUGUI symbolText = symbolObj.AddComponent<TextMeshProUGUI>();
            symbolText.text = symbol;
            symbolText.fontSize = osType == OSType.Windows ? 14 : 10;
            symbolText.color = GetSymbolColor(osType, buttonType);
            symbolText.alignment = TextAlignmentOptions.Center;
            symbolText.verticalAlignment = VerticalAlignmentOptions.Middle;
            symbolText.raycastTarget = false;
            SetRectTransformStretch(symbolObj.GetComponent<RectTransform>());

            return btn;
        }

        private static Color GetButtonColor(OSType osType, ButtonType buttonType)
        {
            switch (osType)
            {
                case OSType.Windows:
                    return buttonType == ButtonType.Close
                        ? new Color(0.9f, 0.2f, 0.2f, 1f)  // Red for close
                        : new Color(0.25f, 0.25f, 0.28f, 1f); // Dark gray for others

                case OSType.Mac:
                    return buttonType switch
                    {
                        ButtonType.Close => new Color(1f, 0.38f, 0.34f, 1f),
                        ButtonType.Minimize => new Color(1f, 0.75f, 0.25f, 1f),
                        ButtonType.Maximize => new Color(0.2f, 0.78f, 0.35f, 1f),
                        _ => Color.gray
                    };

                case OSType.Linux:
                default:
                    return buttonType switch
                    {
                        ButtonType.Close => new Color(1f, 0.4f, 0.5f, 1f),
                        ButtonType.Minimize => new Color(1f, 0.75f, 0.3f, 1f),
                        ButtonType.Maximize => new Color(0.5f, 0.9f, 0.6f, 1f),
                        _ => Color.gray
                    };
            }
        }

        private static string GetButtonSymbol(OSType osType, ButtonType buttonType)
        {
            if (osType == OSType.Windows)
            {
                return buttonType switch
                {
                    ButtonType.Close => "\u2715",      // ✕
                    ButtonType.Minimize => "\u2014",   // —
                    ButtonType.Maximize => "\u25A1",   // □
                    _ => ""
                };
            }
            else
            {
                // Mac/Linux use smaller symbols or none (just colored circles)
                return buttonType switch
                {
                    ButtonType.Close => "\u00D7",      // ×
                    ButtonType.Minimize => "\u2013",   // –
                    ButtonType.Maximize => "+",
                    _ => ""
                };
            }
        }

        private static Color GetSymbolColor(OSType osType, ButtonType buttonType)
        {
            if (osType == OSType.Windows)
            {
                return Color.white;
            }
            else
            {
                // Darker symbols on colored circles
                return new Color(0.15f, 0.1f, 0.1f, 0.7f);
            }
        }

        private static Color GetTitleBarColor(OSType osType)
        {
            return osType switch
            {
                OSType.Windows => new Color(0.2f, 0.4f, 0.6f, 1f),
                OSType.Mac => new Color(0.92f, 0.92f, 0.94f, 0.98f),
                OSType.Linux => new Color(0.35f, 0.15f, 0.45f, 1f),
                _ => Color.gray
            };
        }

        private static Color GetTitleTextColor(OSType osType)
        {
            return osType switch
            {
                OSType.Windows => Color.white,
                OSType.Mac => new Color(0.15f, 0.15f, 0.15f, 1f),
                OSType.Linux => new Color(0.95f, 0.8f, 1f, 1f),
                _ => Color.white
            };
        }

        private static float GetTitleBarHeight(OSType osType)
        {
            return osType switch
            {
                OSType.Mac => 28f,
                _ => 32f
            };
        }

        private static float GetButtonSize(OSType osType)
        {
            return osType switch
            {
                OSType.Windows => 32f,
                OSType.Mac => 12f,
                OSType.Linux => 14f,
                _ => 14f
            };
        }

        private static GameObject CreateBorderFrame(Transform parent, OSType osType)
        {
            GameObject frame = CreateUIElement("BorderFrame", parent);
            frame.transform.SetAsFirstSibling();
            SetRectTransformStretch(frame.GetComponent<RectTransform>());

            float borderWidth = osType == OSType.Linux ? 2f : 1f;
            Color borderColor = osType switch
            {
                OSType.Windows => new Color(0.3f, 0.5f, 0.7f, 1f),
                OSType.Mac => new Color(0.6f, 0.6f, 0.65f, 0.4f),
                OSType.Linux => new Color(0.7f, 0.3f, 0.9f, 0.9f),
                _ => Color.gray
            };

            // Top border
            CreateBorderEdge("TopBorder", frame.transform, borderColor, borderWidth,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), true);

            // Bottom border
            CreateBorderEdge("BottomBorder", frame.transform, borderColor, borderWidth,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), true);

            // Left border
            CreateBorderEdge("LeftBorder", frame.transform, borderColor, borderWidth,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), false);

            // Right border
            CreateBorderEdge("RightBorder", frame.transform, borderColor, borderWidth,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), false);

            return frame;
        }

        private static void CreateBorderEdge(string name, Transform parent, Color color, float width,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, bool horizontal)
        {
            GameObject edge = CreateUIElement(name, parent);
            Image img = edge.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            RectTransform rect = edge.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = horizontal ? new Vector2(0, width) : new Vector2(width, 0);
            rect.anchoredPosition = Vector2.zero;
        }

        private static void CreateResizeHandles(GameObject parent, WindowResizeHandles handlesComponent)
        {
            float edgeSize = 8f;
            float cornerSize = 16f;
            // Slightly visible for debugging - set alpha to 0 for invisible
            Color handleColor = new Color(0.5f, 0.5f, 0.5f, 0.01f);

            // Edge handles - stretch along each edge
            RectTransform topHandle = CreateResizeEdgeHandle("TopHandle", parent.transform, handleColor,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(0, edgeSize), Vector2.zero);

            RectTransform bottomHandle = CreateResizeEdgeHandle("BottomHandle", parent.transform, handleColor,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(0, edgeSize), Vector2.zero);

            RectTransform leftHandle = CreateResizeEdgeHandle("LeftHandle", parent.transform, handleColor,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(edgeSize, 0), Vector2.zero);

            RectTransform rightHandle = CreateResizeEdgeHandle("RightHandle", parent.transform, handleColor,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(edgeSize, 0), Vector2.zero);

            // Corner handles
            RectTransform topLeftHandle = CreateResizeCornerHandle("TopLeftHandle", parent.transform, handleColor,
                new Vector2(0, 1), cornerSize);

            RectTransform topRightHandle = CreateResizeCornerHandle("TopRightHandle", parent.transform, handleColor,
                new Vector2(1, 1), cornerSize);

            RectTransform bottomLeftHandle = CreateResizeCornerHandle("BottomLeftHandle", parent.transform, handleColor,
                new Vector2(0, 0), cornerSize);

            RectTransform bottomRightHandle = CreateResizeCornerHandle("BottomRightHandle", parent.transform, handleColor,
                new Vector2(1, 0), cornerSize);

            // Wire up references
            SerializedObject serializedHandles = new SerializedObject(handlesComponent);
            serializedHandles.FindProperty("topHandle").objectReferenceValue = topHandle;
            serializedHandles.FindProperty("bottomHandle").objectReferenceValue = bottomHandle;
            serializedHandles.FindProperty("leftHandle").objectReferenceValue = leftHandle;
            serializedHandles.FindProperty("rightHandle").objectReferenceValue = rightHandle;
            serializedHandles.FindProperty("topLeftHandle").objectReferenceValue = topLeftHandle;
            serializedHandles.FindProperty("topRightHandle").objectReferenceValue = topRightHandle;
            serializedHandles.FindProperty("bottomLeftHandle").objectReferenceValue = bottomLeftHandle;
            serializedHandles.FindProperty("bottomRightHandle").objectReferenceValue = bottomRightHandle;
            serializedHandles.ApplyModifiedProperties();
        }

        private static RectTransform CreateResizeEdgeHandle(string name, Transform parent, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 sizeDelta, Vector2 anchoredPos)
        {
            GameObject handle = CreateUIElement(name, parent);
            Image img = handle.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            RectTransform rect = handle.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.sizeDelta = sizeDelta;
            rect.anchoredPosition = anchoredPos;

            return rect;
        }

        private static RectTransform CreateResizeCornerHandle(string name, Transform parent, Color color,
            Vector2 anchor, float size)
        {
            GameObject handle = CreateUIElement(name, parent);
            Image img = handle.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            RectTransform rect = handle.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = anchor; // Pivot at the corner
            rect.sizeDelta = new Vector2(size, size);
            rect.anchoredPosition = Vector2.zero;

            return rect;
        }

        private static GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            return obj;
        }

        private static void SetRectTransformStretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        private static void MoveExistingContentToArea(Transform root, Transform contentArea)
        {
            System.Collections.Generic.List<Transform> toMove = new System.Collections.Generic.List<Transform>();

            foreach (Transform child in root)
            {
                string childName = child.name;
                if (childName != "BorderFrame" && childName != "TitleBar" &&
                    childName != "ContentArea" && childName != "ResizeHandles" &&
                    childName != "WindowControls")
                {
                    toMove.Add(child);
                }
            }

            foreach (Transform child in toMove)
            {
                child.SetParent(contentArea, false);
            }
        }
    }
}
