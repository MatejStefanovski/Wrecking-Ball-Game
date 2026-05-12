using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Text timerText;
    [SerializeField] private Text blocksText;
    [SerializeField] private Text levelText;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject gameOverScreen;

    [Header("Settings")]
    [SerializeField] private bool autoCreateUiObjects = true;

    private static Font cachedFont;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EnsureEditorUiManager()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying ||
                EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (Object.FindAnyObjectByType<UIManager>() != null)
            {
                return;
            }

            GameObject go = new GameObject("UIManager");
            go.AddComponent<UIManager>();
        };
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void EnsureRuntimeUiManager()
    {
        if (Object.FindAnyObjectByType<UIManager>() != null)
        {
            return;
        }

        GameObject go = new GameObject("UIManager");
        go.AddComponent<UIManager>();
    }

    private void Awake()
    {
        if (autoCreateUiObjects)
        {
            EnsureUiObjects();
        }
    }

    private void OnEnable()
    {
        if (autoCreateUiObjects)
        {
            EnsureUiObjects();
        }
    }

    public void UpdateTimer(float timeValue)
    {
        if (timerText == null)
            return;

        int displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, timeValue));
        timerText.text = $"Time: {displaySeconds}";
    }

    public void UpdateBlocksRemaining(int remaining)
    {
        if (blocksText == null)
            return;

        blocksText.text = $"Blocks: {Mathf.Max(0, remaining)}";
    }

    public void HideEndScreens()
    {
        if (winScreen != null)
            winScreen.SetActive(false);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(false);
    }

    public void ShowEndScreen(bool isWin)
    {
        if (winScreen != null)
            winScreen.SetActive(isWin);

        if (gameOverScreen != null)
            gameOverScreen.SetActive(!isWin);
    }

    public void UpdateLevel(int level)
    {
        if (levelText == null)
            return;

        levelText.text = $"Wins: {level}";
    }
    private void EnsureUiObjects()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("GameUI");

            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        if (levelText == null)
        {
            levelText = CreateText(
                "LevelText",
                canvas.transform,
                new Vector2(120f, -110f),
                "Wins: 0",
                26,
                TextAnchor.MiddleLeft
            );
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        if (timerText == null)
        {
            timerText = CreateText(
                "TimerText",
                canvas.transform,
                new Vector2(120f, -30f),
                "Time: 60",
                26,
                TextAnchor.MiddleLeft
            );
        }

        if (blocksText == null)
        {
            blocksText = CreateText(
                "BlocksText",
                canvas.transform,
                new Vector2(120f, -70f),
                "Blocks: 0",
                26,
                TextAnchor.MiddleLeft
            );
        }

        if (winScreen == null)
        {
            winScreen = CreateCenterPanel(
                "WinPanel",
                canvas.transform,
                "YOU WIN"
            );
        }

        if (gameOverScreen == null)
        {
            gameOverScreen = CreateCenterPanel(
                "GameOverPanel",
                canvas.transform,
                "GAME OVER"
            );
        }
    }

    private static Text CreateText(
        string name,
        Transform parent,
        Vector2 anchoredPosition,
        string value,
        int fontSize,
        TextAnchor align)
    {
        GameObject textGo = GameObject.Find(name);

        if (textGo == null)
        {
            textGo = new GameObject(name);
            textGo.transform.SetParent(parent, false);
        }

        RectTransform rt = textGo.GetComponent<RectTransform>();

        if (rt == null)
        {
            rt = textGo.AddComponent<RectTransform>();
        }

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = anchoredPosition;
        rt.sizeDelta = new Vector2(300f, 50f);

        Text text = textGo.GetComponent<Text>();

        if (text == null)
        {
            text = textGo.AddComponent<Text>();
        }

        text.font = GetBuiltinFont();
        text.fontSize = fontSize;
        text.color = Color.white;
        text.alignment = align;
        text.text = value;
        text.raycastTarget = false;

        return text;
    }

    private static GameObject CreateCenterPanel(
        string name,
        Transform parent,
        string label)
    {
        GameObject panelGo = GameObject.Find(name);

        if (panelGo == null)
        {
            panelGo = new GameObject(name);
            panelGo.transform.SetParent(parent, false);
        }

        RectTransform rt = panelGo.GetComponent<RectTransform>();

        if (rt == null)
        {
            rt = panelGo.AddComponent<RectTransform>();
        }

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(400f, 150f);

        Image img = panelGo.GetComponent<Image>();

        if (img == null)
        {
            img = panelGo.AddComponent<Image>();
        }

        img.color = new Color(0f, 0f, 0f, 0.75f);

        Text txt = CreateText(
            name + "_Text",
            panelGo.transform,
            Vector2.zero,
            label,
            42,
            TextAnchor.MiddleCenter
        );

        RectTransform txtRt = txt.rectTransform;
        txtRt.anchorMin = new Vector2(0.5f, 0.5f);
        txtRt.anchorMax = new Vector2(0.5f, 0.5f);
        txtRt.pivot = new Vector2(0.5f, 0.5f);
        txtRt.anchoredPosition = Vector2.zero;
        txtRt.sizeDelta = new Vector2(360f, 100f);

        panelGo.SetActive(false);

        return panelGo;
    }

    private static Font GetBuiltinFont()
    {
        if (cachedFont != null)
        {
            return cachedFont;
        }

        cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (cachedFont == null)
        {
            Debug.LogError(
                "Could not load built-in font. " +
                "Try importing TMP Essentials."
            );
        }

        return cachedFont;
    }
}