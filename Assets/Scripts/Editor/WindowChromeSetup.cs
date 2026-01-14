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

            // Dreamwave Linux: Purple gradient, circular controls on left
            style.titleBarHeight = 32f;
            style.titleBarFocusedColor = new Color(0.42f, 0.23f, 0.49f, 1f);    // #6B3A7D purple
            style.titleBarUnfocusedColor = new Color(0.18f, 0.11f, 0.31f, 1f);  // #2D1B4E dark purple
            style.titleTextColor = new Color(0.9f, 0.85f, 0.95f, 1f);           // Light lavender
            style.titleFontSize = 14f;
            style.titleFontStyle = FontStyles.Normal;

            // Control buttons - macOS-like traffic lights but with dreamwave colors
            style.controlPosition = ControlButtonPosition.Left;
            style.controlButtonSize = 14f;
            style.controlButtonSpacing = 8f;
            style.controlButtonPadding = 12f;

            style.closeButtonColor = new Color(1f, 0.47f, 0.47f, 1f);           // Coral red
            style.minimizeButtonColor = new Color(1f, 0.84f, 0.4f, 1f);         // Warm yellow
            style.maximizeButtonColor = new Color(0.4f, 0.87f, 0.67f, 1f);      // Mint green

            // Border - subtle neon glow
            style.borderWidth = 2f;
            style.borderFocusedColor = new Color(0.6f, 0.4f, 0.8f, 0.8f);       // Purple glow
            style.borderUnfocusedColor = new Color(0.3f, 0.2f, 0.4f, 0.5f);     // Dim purple

            // Resize handles
            style.resizeHandleSize = 6f;
            style.resizeHandleHoverColor = new Color(0.8f, 0.5f, 1f, 0.6f);

            // Animation
            style.focusAnimationDuration = 0.15f;

            SaveStyleAsset(style, "LinuxWindowStyle");
        }

        [MenuItem("SampleOS/Window Chrome/Create Windows Style")]
        public static void CreateWindowsStyle()
        {
            var style = ScriptableObject.CreateInstance<WindowChromeStyle>();

            // Windows: Blue gradient, square controls on right
            style.titleBarHeight = 32f;
            style.titleBarFocusedColor = new Color(0.29f, 0.48f, 0.65f, 1f);    // #4A7BA7 blue
            style.titleBarUnfocusedColor = new Color(0.23f, 0.35f, 0.55f, 1f);  // #3A5A8C darker blue
            style.titleTextColor = Color.white;
            style.titleFontSize = 13f;
            style.titleFontStyle = FontStyles.Normal;

            // Control buttons - Windows style on right
            style.controlPosition = ControlButtonPosition.Right;
            style.controlButtonSize = 46f;
            style.controlButtonSpacing = 0f;
            style.controlButtonPadding = 0f;

            style.closeButtonColor = new Color(0.9f, 0.3f, 0.3f, 1f);           // Red
            style.minimizeButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);        // Gray
            style.maximizeButtonColor = new Color(0.5f, 0.5f, 0.5f, 1f);        // Gray

            // Border - clean rounded
            style.borderWidth = 1f;
            style.borderFocusedColor = new Color(0.29f, 0.48f, 0.65f, 1f);
            style.borderUnfocusedColor = new Color(0.4f, 0.4f, 0.4f, 0.8f);

            // Resize handles
            style.resizeHandleSize = 5f;
            style.resizeHandleHoverColor = new Color(0.29f, 0.48f, 0.65f, 0.5f);

            // Animation
            style.focusAnimationDuration = 0.1f;

            SaveStyleAsset(style, "WindowsWindowStyle");
        }

        [MenuItem("SampleOS/Window Chrome/Create Mac Style")]
        public static void CreateMacStyle()
        {
            var style = ScriptableObject.CreateInstance<WindowChromeStyle>();

            // Mac: Frosted pink/white, traffic light controls on left
            style.titleBarHeight = 28f;
            style.titleBarFocusedColor = new Color(0.95f, 0.92f, 0.95f, 0.95f); // Frosted white-pink
            style.titleBarUnfocusedColor = new Color(0.9f, 0.88f, 0.9f, 0.9f);  // Slightly dimmer
            style.titleTextColor = new Color(0.2f, 0.2f, 0.2f, 1f);             // Dark gray text
            style.titleFontSize = 13f;
            style.titleFontStyle = FontStyles.Normal;

            // Control buttons - macOS traffic lights on left
            style.controlPosition = ControlButtonPosition.Left;
            style.controlButtonSize = 12f;
            style.controlButtonSpacing = 8f;
            style.controlButtonPadding = 10f;

            style.closeButtonColor = new Color(1f, 0.38f, 0.38f, 1f);           // macOS red
            style.minimizeButtonColor = new Color(1f, 0.74f, 0.18f, 1f);        // macOS yellow
            style.maximizeButtonColor = new Color(0.16f, 0.78f, 0.35f, 1f);     // macOS green

            // Border - soft shadow effect
            style.borderWidth = 1f;
            style.borderFocusedColor = new Color(0.7f, 0.7f, 0.7f, 0.5f);
            style.borderUnfocusedColor = new Color(0.6f, 0.6f, 0.6f, 0.3f);

            // Resize handles
            style.resizeHandleSize = 4f;
            style.resizeHandleHoverColor = new Color(0.3f, 0.3f, 0.3f, 0.3f);

            // Animation
            style.focusAnimationDuration = 0.2f;

            SaveStyleAsset(style, "MacWindowStyle");
        }

        private static void SaveStyleAsset(WindowChromeStyle style, string name)
        {
            // Ensure directory exists
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

        [MenuItem("SampleOS/Window Chrome/Add Chrome to Selected Prefab")]
        public static void AddChromeToSelectedPrefab()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                Debug.LogError("[WindowChromeSetup] No GameObject selected. Select a prefab root in the Project or Hierarchy.");
                return;
            }

            // Check if already has WindowChrome
            if (selected.GetComponent<WindowChrome>() != null)
            {
                Debug.LogWarning("[WindowChromeSetup] Selected object already has WindowChrome component.");
                return;
            }

            AddWindowChromeHierarchy(selected);
            Debug.Log($"[WindowChromeSetup] Added window chrome to {selected.name}");
        }

        /// <summary>
        /// Adds the window chrome hierarchy to an existing app prefab.
        /// </summary>
        public static void AddWindowChromeHierarchy(GameObject root)
        {
            // Get or add RectTransform
            RectTransform rootRect = root.GetComponent<RectTransform>();
            if (rootRect == null)
            {
                rootRect = root.AddComponent<RectTransform>();
            }

            // Get or add CanvasGroup for minimize fade
            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = root.AddComponent<CanvasGroup>();
            }

            // Add WindowChrome component
            WindowChrome chrome = root.AddComponent<WindowChrome>();

            // Create Border Frame (4 edges, not a solid fill)
            GameObject borderFrame = CreateBorderFrame(root.transform);

            // Create TitleBar
            GameObject titleBar = CreateUIElement("TitleBar", root.transform);
            titleBar.transform.SetSiblingIndex(1);
            Image titleBarBg = titleBar.AddComponent<Image>();
            titleBarBg.color = new Color(0.42f, 0.23f, 0.49f, 1f);
            RectTransform titleBarRect = titleBar.GetComponent<RectTransform>();
            titleBarRect.anchorMin = new Vector2(0, 1);
            titleBarRect.anchorMax = new Vector2(1, 1);
            titleBarRect.pivot = new Vector2(0.5f, 1);
            titleBarRect.sizeDelta = new Vector2(0, 32);
            titleBarRect.anchoredPosition = Vector2.zero;

            // Create Title Text
            GameObject titleTextObj = CreateUIElement("TitleText", titleBar.transform);
            TextMeshProUGUI titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "Window Title";
            titleText.fontSize = 14;
            titleText.color = new Color(0.9f, 0.85f, 0.95f, 1f);
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.verticalAlignment = VerticalAlignmentOptions.Middle;
            SetRectTransformStretch(titleTextObj.GetComponent<RectTransform>());

            // Create Window Controls container
            GameObject controls = CreateUIElement("WindowControls", titleBar.transform);
            RectTransform controlsRect = controls.GetComponent<RectTransform>();
            controlsRect.anchorMin = new Vector2(0, 0.5f);
            controlsRect.anchorMax = new Vector2(0, 0.5f);
            controlsRect.pivot = new Vector2(0, 0.5f);
            controlsRect.anchoredPosition = new Vector2(12, 0);
            controlsRect.sizeDelta = new Vector2(70, 20);
            HorizontalLayoutGroup layout = controls.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            // Create control buttons with symbols
            GameObject closeBtn = CreateCircleButton("CloseButton", controls.transform, new Color(1f, 0.47f, 0.47f), "\u00D7"); // ×
            GameObject minBtn = CreateCircleButton("MinimizeButton", controls.transform, new Color(1f, 0.84f, 0.4f), "\u2013"); // –
            GameObject maxBtn = CreateCircleButton("MaximizeButton", controls.transform, new Color(0.4f, 0.87f, 0.67f), "+");

            // Create ContentArea (where app content goes)
            GameObject contentArea = CreateUIElement("ContentArea", root.transform);
            contentArea.transform.SetSiblingIndex(2);
            RectTransform contentRect = contentArea.GetComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(2, 2);      // Border padding
            contentRect.offsetMax = new Vector2(-2, -34);   // Title bar + border

            // Move existing content into ContentArea
            MoveExistingContentToArea(root.transform, contentArea.transform);

            // Create ResizeHandles with actual handle GameObjects
            GameObject resizeHandlesObj = CreateUIElement("ResizeHandles", root.transform);
            WindowResizeHandles handles = resizeHandlesObj.AddComponent<WindowResizeHandles>();
            SetRectTransformStretch(resizeHandlesObj.GetComponent<RectTransform>());

            // Create the 8 resize handles
            CreateResizeHandles(resizeHandlesObj, handles);

            // Get the border image from the frame
            Image borderImage = borderFrame.transform.Find("TopBorder")?.GetComponent<Image>();

            // Wire up serialized references via SerializedObject
            SerializedObject serializedChrome = new SerializedObject(chrome);
            serializedChrome.FindProperty("windowRect").objectReferenceValue = rootRect;
            serializedChrome.FindProperty("titleBar").objectReferenceValue = titleBarRect;
            serializedChrome.FindProperty("contentArea").objectReferenceValue = contentRect;
            serializedChrome.FindProperty("canvasGroup").objectReferenceValue = canvasGroup;
            serializedChrome.FindProperty("closeButton").objectReferenceValue = closeBtn.GetComponent<Button>();
            serializedChrome.FindProperty("minimizeButton").objectReferenceValue = minBtn.GetComponent<Button>();
            serializedChrome.FindProperty("maximizeButton").objectReferenceValue = maxBtn.GetComponent<Button>();
            serializedChrome.FindProperty("titleText").objectReferenceValue = titleText;
            serializedChrome.FindProperty("titleBarBackground").objectReferenceValue = titleBarBg;
            if (borderImage != null)
                serializedChrome.FindProperty("borderImage").objectReferenceValue = borderImage;
            serializedChrome.FindProperty("resizeHandles").objectReferenceValue = handles;
            serializedChrome.ApplyModifiedProperties();

            Debug.Log("[WindowChromeSetup] Window chrome hierarchy created successfully");
        }

        private static GameObject CreateBorderFrame(Transform parent)
        {
            GameObject frame = CreateUIElement("BorderFrame", parent);
            frame.transform.SetAsFirstSibling();
            SetRectTransformStretch(frame.GetComponent<RectTransform>());

            float borderWidth = 2f;
            Color borderColor = new Color(0.6f, 0.4f, 0.8f, 0.8f);

            // Top border
            GameObject top = CreateUIElement("TopBorder", frame.transform);
            Image topImg = top.AddComponent<Image>();
            topImg.color = borderColor;
            topImg.raycastTarget = false;
            RectTransform topRect = top.GetComponent<RectTransform>();
            topRect.anchorMin = new Vector2(0, 1);
            topRect.anchorMax = new Vector2(1, 1);
            topRect.pivot = new Vector2(0.5f, 1);
            topRect.sizeDelta = new Vector2(0, borderWidth);
            topRect.anchoredPosition = Vector2.zero;

            // Bottom border
            GameObject bottom = CreateUIElement("BottomBorder", frame.transform);
            Image bottomImg = bottom.AddComponent<Image>();
            bottomImg.color = borderColor;
            bottomImg.raycastTarget = false;
            RectTransform bottomRect = bottom.GetComponent<RectTransform>();
            bottomRect.anchorMin = new Vector2(0, 0);
            bottomRect.anchorMax = new Vector2(1, 0);
            bottomRect.pivot = new Vector2(0.5f, 0);
            bottomRect.sizeDelta = new Vector2(0, borderWidth);
            bottomRect.anchoredPosition = Vector2.zero;

            // Left border
            GameObject left = CreateUIElement("LeftBorder", frame.transform);
            Image leftImg = left.AddComponent<Image>();
            leftImg.color = borderColor;
            leftImg.raycastTarget = false;
            RectTransform leftRect = left.GetComponent<RectTransform>();
            leftRect.anchorMin = new Vector2(0, 0);
            leftRect.anchorMax = new Vector2(0, 1);
            leftRect.pivot = new Vector2(0, 0.5f);
            leftRect.sizeDelta = new Vector2(borderWidth, 0);
            leftRect.anchoredPosition = Vector2.zero;

            // Right border
            GameObject right = CreateUIElement("RightBorder", frame.transform);
            Image rightImg = right.AddComponent<Image>();
            rightImg.color = borderColor;
            rightImg.raycastTarget = false;
            RectTransform rightRect = right.GetComponent<RectTransform>();
            rightRect.anchorMin = new Vector2(1, 0);
            rightRect.anchorMax = new Vector2(1, 1);
            rightRect.pivot = new Vector2(1, 0.5f);
            rightRect.sizeDelta = new Vector2(borderWidth, 0);
            rightRect.anchoredPosition = Vector2.zero;

            return frame;
        }

        private static void CreateResizeHandles(GameObject parent, WindowResizeHandles handlesComponent)
        {
            float edgeSize = 6f;
            float cornerSize = 12f;
            Color handleColor = new Color(1f, 1f, 1f, 0f); // Transparent but raycast-able

            // Edge handles
            RectTransform topHandle = CreateEdgeHandle("TopHandle", parent.transform, handleColor,
                new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1),
                new Vector2(-cornerSize * 2, edgeSize), new Vector2(0, 0));

            RectTransform bottomHandle = CreateEdgeHandle("BottomHandle", parent.transform, handleColor,
                new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
                new Vector2(-cornerSize * 2, edgeSize), new Vector2(0, 0));

            RectTransform leftHandle = CreateEdgeHandle("LeftHandle", parent.transform, handleColor,
                new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f),
                new Vector2(edgeSize, -cornerSize * 2), new Vector2(0, 0));

            RectTransform rightHandle = CreateEdgeHandle("RightHandle", parent.transform, handleColor,
                new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f),
                new Vector2(edgeSize, -cornerSize * 2), new Vector2(0, 0));

            // Corner handles
            RectTransform topLeftHandle = CreateCornerHandle("TopLeftHandle", parent.transform, handleColor,
                new Vector2(0, 1), new Vector2(0, 1), cornerSize);

            RectTransform topRightHandle = CreateCornerHandle("TopRightHandle", parent.transform, handleColor,
                new Vector2(1, 1), new Vector2(1, 1), cornerSize);

            RectTransform bottomLeftHandle = CreateCornerHandle("BottomLeftHandle", parent.transform, handleColor,
                new Vector2(0, 0), new Vector2(0, 0), cornerSize);

            RectTransform bottomRightHandle = CreateCornerHandle("BottomRightHandle", parent.transform, handleColor,
                new Vector2(1, 0), new Vector2(1, 0), cornerSize);

            // Wire up serialized references
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

        private static RectTransform CreateEdgeHandle(string name, Transform parent, Color color,
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

        private static RectTransform CreateCornerHandle(string name, Transform parent, Color color,
            Vector2 anchor, Vector2 pivot, float size)
        {
            GameObject handle = CreateUIElement(name, parent);
            Image img = handle.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = true;

            RectTransform rect = handle.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
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

        private static GameObject CreateCircleButton(string name, Transform parent, Color bgColor, string symbol)
        {
            GameObject btn = CreateUIElement(name, parent);
            Image img = btn.AddComponent<Image>();
            img.color = bgColor;
            btn.AddComponent<Button>();

            RectTransform rect = btn.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(14, 14);

            // Add symbol text
            GameObject symbolObj = CreateUIElement("Symbol", btn.transform);
            TextMeshProUGUI symbolText = symbolObj.AddComponent<TextMeshProUGUI>();
            symbolText.text = symbol;
            symbolText.fontSize = 12;
            symbolText.color = new Color(0.2f, 0.1f, 0.1f, 0.8f); // Dark semi-transparent
            symbolText.alignment = TextAlignmentOptions.Center;
            symbolText.verticalAlignment = VerticalAlignmentOptions.Middle;
            symbolText.raycastTarget = false;
            SetRectTransformStretch(symbolObj.GetComponent<RectTransform>());

            return btn;
        }

        private static void MoveExistingContentToArea(Transform root, Transform contentArea)
        {
            // Find children that aren't part of chrome
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
