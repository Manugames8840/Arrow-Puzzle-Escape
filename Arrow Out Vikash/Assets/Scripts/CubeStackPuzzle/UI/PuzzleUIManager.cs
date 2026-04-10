using UnityEngine;
using UnityEngine.UI;

namespace CubeStackPuzzle
{
    /// <summary>
    /// Creates the puzzle UI programmatically.
    /// Has a SINGLE spawn button that spawns a random destruction character.
    /// Also shows slot status, game over, and level clear panels.
    /// </summary>
    public class PuzzleUIManager : MonoBehaviour
    {
        // ── Colors ─────────────────────────────────────────────────────────
        [Header("── UI Colors ──────────────────────────────────")]
        [SerializeField] private Color panelColor        = new Color(0.1f, 0.1f, 0.12f, 0.9f);
        [SerializeField] private Color spawnButtonColor  = new Color(0.15f, 0.55f, 0.85f, 0.95f);
        [SerializeField] private Color slotFreeColor     = new Color(0.2f, 0.7f, 0.3f, 1f);
        [SerializeField] private Color slotOccupiedColor = new Color(0.9f, 0.3f, 0.2f, 1f);
        [SerializeField] private Color slotLockedColor   = new Color(0.3f, 0.3f, 0.3f, 0.6f);

        // ── Internal ───────────────────────────────────────────────────────
        private Canvas    _canvas;
        private Image[]   _slotIndicators;
        private GameObject _gameOverPanel;
        private GameObject _levelClearPanel;

        // =====================================================================
        // UNITY LIFECYCLE
        // =====================================================================

        private void Start()
        {
            CreateUI();

            // Subscribe to events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver        += ShowGameOver;
                GameManager.Instance.OnLevelCleared    += ShowLevelClear;
                GameManager.Instance.OnBoardGenerated  += RefreshSlotDisplay;
                GameManager.Instance.OnCharacterSpawned += RefreshSlotDisplay;
            }

            if (SlotManager.Instance != null)
            {
                SlotManager.Instance.OnSlotOccupied  += _ => RefreshSlotDisplay();
                SlotManager.Instance.OnSlotReleased  += _ => RefreshSlotDisplay();
                SlotManager.Instance.OnSlotUnlocked  += _ => RefreshSlotDisplay();
            }

            RefreshSlotDisplay();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver        -= ShowGameOver;
                GameManager.Instance.OnLevelCleared    -= ShowLevelClear;
                GameManager.Instance.OnBoardGenerated  -= RefreshSlotDisplay;
                GameManager.Instance.OnCharacterSpawned -= RefreshSlotDisplay;
            }
        }

        // =====================================================================
        // UI CREATION
        // =====================================================================

        private void CreateUI()
        {
            // ── Canvas ──
            GameObject canvasGO = new GameObject("PuzzleUI_Canvas");
            canvasGO.transform.SetParent(transform);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight  = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();

            // ── Slot Status Bar (top) ──
            CreateSlotDisplay(canvasGO.transform);

            // ── Single Spawn Button (bottom) ──
            CreateSpawnButton(canvasGO.transform);

            // ── Game Over Panel (hidden) ──
            _gameOverPanel = CreateOverlayPanel(canvasGO.transform, "GameOverPanel",
                "GAME OVER", new Color(0.8f, 0.15f, 0.1f, 0.95f));

            // ── Level Clear Panel (hidden) ──
            _levelClearPanel = CreateOverlayPanel(canvasGO.transform, "LevelClearPanel",
                "LEVEL CLEAR!", new Color(0.1f, 0.7f, 0.3f, 0.95f));
        }

        // ─────────────────────────────────────────────────────────────────────
        // SLOT DISPLAY
        // ─────────────────────────────────────────────────────────────────────

        private void CreateSlotDisplay(Transform parent)
        {
            int slotCount = SlotManager.Instance != null ? SlotManager.Instance.TotalSlots : 6;

            // Container
            GameObject container = new GameObject("SlotDisplay");
            container.transform.SetParent(parent, false);

            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchorMin          = new Vector2(0.1f, 1f);
            containerRT.anchorMax          = new Vector2(0.9f, 1f);
            containerRT.pivot              = new Vector2(0.5f, 1f);
            containerRT.anchoredPosition   = new Vector2(0, -30f);
            containerRT.sizeDelta          = new Vector2(0, 70f);

            Image containerBg = container.AddComponent<Image>();
            containerBg.color = panelColor;

            HorizontalLayoutGroup layout = container.AddComponent<HorizontalLayoutGroup>();
            layout.spacing                = 12f;
            layout.padding                = new RectOffset(20, 20, 10, 10);
            layout.childAlignment         = TextAnchor.MiddleCenter;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = true;

            // "Slots:" label
            CreateLabel(container.transform, "Slots:", 28, FontStyle.Bold, 100f);

            // Slot indicators
            _slotIndicators = new Image[slotCount];
            for (int i = 0; i < slotCount; i++)
            {
                GameObject slotGO = new GameObject($"Slot_{i}");
                slotGO.transform.SetParent(container.transform, false);

                slotGO.AddComponent<RectTransform>().sizeDelta = new Vector2(45, 45);

                LayoutElement le = slotGO.AddComponent<LayoutElement>();
                le.preferredWidth  = 45;
                le.preferredHeight = 45;

                Image img = slotGO.AddComponent<Image>();
                img.color = slotFreeColor;
                _slotIndicators[i] = img;
            }

            // Unlock button
            CreateButton(container.transform, "Unlock", new Color(0.2f, 0.5f, 0.9f, 0.9f),
                22, 120f, OnUnlockClicked);
        }

        // ─────────────────────────────────────────────────────────────────────
        // SINGLE SPAWN BUTTON
        // ─────────────────────────────────────────────────────────────────────

        private void CreateSpawnButton(Transform parent)
        {
            // Container
            GameObject container = new GameObject("SpawnButtonPanel");
            container.transform.SetParent(parent, false);

            RectTransform containerRT = container.AddComponent<RectTransform>();
            containerRT.anchorMin        = new Vector2(0.2f, 0f);
            containerRT.anchorMax        = new Vector2(0.8f, 0f);
            containerRT.pivot            = new Vector2(0.5f, 0f);
            containerRT.anchoredPosition = new Vector2(0, 40f);
            containerRT.sizeDelta        = new Vector2(0, 100f);

            Image containerBg = container.AddComponent<Image>();
            containerBg.color = panelColor;

            // The single spawn button
            GameObject btnGO = new GameObject("Btn_Spawn");
            btnGO.transform.SetParent(container.transform, false);

            RectTransform btnRT = btnGO.AddComponent<RectTransform>();
            btnRT.anchorMin = new Vector2(0.1f, 0.1f);
            btnRT.anchorMax = new Vector2(0.9f, 0.9f);
            btnRT.offsetMin = Vector2.zero;
            btnRT.offsetMax = Vector2.zero;

            Image btnBg = btnGO.AddComponent<Image>();
            btnBg.color = spawnButtonColor;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnBg;

            ColorBlock colors   = btn.colors;
            colors.normalColor      = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f);
            colors.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
            btn.colors = colors;

            btn.onClick.AddListener(OnSpawnClicked);

            // Button label
            GameObject textGO = new GameObject("Label");
            textGO.transform.SetParent(btnGO.transform, false);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = new Vector2(8, 4);
            textRT.offsetMax = new Vector2(-8, -4);

            Text txt = textGO.AddComponent<Text>();
            txt.text               = "SPAWN CHARACTER";
            txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize           = 34;
            txt.color              = Color.white;
            txt.alignment          = TextAnchor.MiddleCenter;
            txt.fontStyle          = FontStyle.Bold;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize  = 20;
            txt.resizeTextMaxSize  = 34;
        }

        // ─────────────────────────────────────────────────────────────────────
        // OVERLAY PANELS
        // ─────────────────────────────────────────────────────────────────────

        private GameObject CreateOverlayPanel(Transform parent, string name,
            string message, Color boxColor)
        {
            // Full-screen dark overlay
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);

            RectTransform panelRT = panel.AddComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero;
            panelRT.offsetMax = Vector2.zero;

            Image panelBg = panel.AddComponent<Image>();
            panelBg.color = new Color(0f, 0f, 0f, 0.7f);

            // Center box
            GameObject box = new GameObject("Box");
            box.transform.SetParent(panel.transform, false);

            RectTransform boxRT = box.AddComponent<RectTransform>();
            boxRT.anchorMin = new Vector2(0.15f, 0.35f);
            boxRT.anchorMax = new Vector2(0.85f, 0.65f);
            boxRT.offsetMin = Vector2.zero;
            boxRT.offsetMax = Vector2.zero;

            Image boxBg = box.AddComponent<Image>();
            boxBg.color = boxColor;

            // Message
            GameObject textGO = new GameObject("Message");
            textGO.transform.SetParent(box.transform, false);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0, 0.45f);
            textRT.anchorMax = new Vector2(1, 0.9f);
            textRT.offsetMin = new Vector2(10, 0);
            textRT.offsetMax = new Vector2(-10, 0);

            Text txt = textGO.AddComponent<Text>();
            txt.text               = message;
            txt.font               = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize           = 50;
            txt.color              = Color.white;
            txt.alignment          = TextAnchor.MiddleCenter;
            txt.fontStyle          = FontStyle.Bold;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize  = 28;
            txt.resizeTextMaxSize  = 50;

            // Restart button
            CreateButton(box.transform, "RESTART", new Color(0.2f, 0.2f, 0.2f, 0.9f),
                28, -1f, OnRestartClicked,
                new Vector2(0.25f, 0.08f), new Vector2(0.75f, 0.38f));

            panel.SetActive(false);
            return panel;
        }

        // =====================================================================
        // HELPERS
        // =====================================================================

        private void CreateLabel(Transform parent, string text, int fontSize, FontStyle style, float prefWidth)
        {
            GameObject go = new GameObject("Label");
            go.transform.SetParent(parent, false);

            go.AddComponent<RectTransform>().sizeDelta = new Vector2(prefWidth, 50);

            LayoutElement le = go.AddComponent<LayoutElement>();
            le.preferredWidth = prefWidth;

            Text t = go.AddComponent<Text>();
            t.text      = text;
            t.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize  = fontSize;
            t.color     = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.fontStyle = style;
        }

        private void CreateButton(Transform parent, string label, Color bgColor,
            int fontSize, float prefWidth, UnityEngine.Events.UnityAction onClick,
            Vector2 anchorMin = default, Vector2 anchorMax = default)
        {
            GameObject btnGO = new GameObject($"Btn_{label}");
            btnGO.transform.SetParent(parent, false);

            RectTransform rt = btnGO.AddComponent<RectTransform>();

            if (anchorMin != default || anchorMax != default)
            {
                // Anchor-based positioning
                rt.anchorMin = anchorMin;
                rt.anchorMax = anchorMax;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
            else
            {
                rt.sizeDelta = new Vector2(prefWidth, 50);
                LayoutElement le = btnGO.AddComponent<LayoutElement>();
                le.preferredWidth = prefWidth;
            }

            Image bg = btnGO.AddComponent<Image>();
            bg.color = bgColor;

            Button btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);

            // Label
            GameObject textGO = new GameObject("Label");
            textGO.transform.SetParent(btnGO.transform, false);

            RectTransform textRT = textGO.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            Text txt = textGO.AddComponent<Text>();
            txt.text      = label;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize  = fontSize;
            txt.color     = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontStyle = FontStyle.Bold;
        }

        // =====================================================================
        // CALLBACKS
        // =====================================================================

        private void OnSpawnClicked()
        {
            GameManager.Instance?.SpawnRandomCharacter();
        }

        private void OnUnlockClicked()
        {
            SlotManager.Instance?.UnlockNextLockedSlot();
        }

        private void OnRestartClicked()
        {
            _gameOverPanel?.SetActive(false);
            _levelClearPanel?.SetActive(false);
            GameManager.Instance?.RestartLevel();
            RefreshSlotDisplay();
        }

        // =====================================================================
        // DISPLAY UPDATES
        // =====================================================================

        private void RefreshSlotDisplay()
        {
            if (_slotIndicators == null || SlotManager.Instance == null) return;

            var slots = SlotManager.Instance.Slots;
            for (int i = 0; i < _slotIndicators.Length && i < slots.Length; i++)
            {
                if (slots[i].isLocked)
                    _slotIndicators[i].color = slotLockedColor;
                else if (slots[i].isOccupied)
                    _slotIndicators[i].color = slotOccupiedColor;
                else
                    _slotIndicators[i].color = slotFreeColor;
            }
        }

        private void ShowGameOver()  => _gameOverPanel?.SetActive(true);
        private void ShowLevelClear() => _levelClearPanel?.SetActive(true);
    }
}
