using UnityEngine.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class UIManager : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private Text blocksText;
    [SerializeField] private GameObject winScreen;
    [SerializeField] private GameObject gameOverScreen;
    [Header("Auto UI Objects")]
    [SerializeField] private bool autoCreateUiObjects = true;

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void EnsureEditorUiManager()
    {
        EditorApplication.delayCall += () =>
        {
            if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (FindFirstObjectByType<UIManager>() != null)
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
        if (FindFirstObjectByType<UIManager>() != null)
        {
            return;
        }

        GameObject go = new GameObject("UIManager");
        go.AddComponent<UIManager>();
    }

    private void OnEnable()
    {
        if (autoCreateUiObjects)
        {
            EnsureUiObjects();
        }
    }

    private void Awake()
    {
        if (autoCreateUiObjects)
        {
            EnsureUiObjects();
        }
    }

    public void UpdateTimer(float timeValue)
    {
        if (timerText == null)
        {
            return;
        }

        int displaySeconds = Mathf.CeilToInt(Mathf.Max(0f, timeValue));
        timerText.text = $"Time: {displaySeconds}";
    }

    public void UpdateBlocksRemaining(int remaining)
    {
        if (blocksText == null)
        {
            return;
        }

        blocksText.text = $"Blocks: {Mathf.Max(0, remaining)}";
    }

    public void HideEndScreens()
    {
        if (winScreen != null)
        {
            winScreen.SetActive(false);
        }

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(false);
        }
    }

    public void ShowEndScreen(bool isWin)
    {
        if (winScreen != null)
        {
            winScreen.SetActive(isWin);
        }

        if (gameOverScreen != null)
        {
            gameOverScreen.SetActive(!isWin);
        }
    }

    private void EnsureUiObjects()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("GameUI");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();
        }
        else
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        canvas.overrideSorting = true;
        canvas.sortingOrder = 500;

        if (timerText == null)
        {
            timerText = CreateText("TimerText", canvas.transform, new Vector2(120f, -30f), "Time: 60", 26, TextAnchor.MiddleLeft);
        }

        if (blocksText == null)
        {
            blocksText = CreateText("BlocksText", canvas.transform, new Vector2(120f, -65f), "Blocks: 0", 26, TextAnchor.MiddleLeft);
        }

        if (winScreen == null)
        {
            winScreen = CreateCenterPanel("WinPanel", canvas.transform, "YOU WIN");
        }

        if (gameOverScreen == null)
        {
            gameOverScreen = CreateCenterPanel("GameOverPanel", canvas.transform, "GAME OVER");
        }
    }

    private static Text CreateText(string name, Transform parent, Vector2 anchoredPosition, string value, int fontSize, TextAnchor align)
    {
        GameObject existing = GameObject.Find(name);
        GameObject textGo = existing != null ? existing : new GameObject(name);
        bool isNew = existing == null;
        textGo.transform.SetParent(parent, false);

        RectTransform rt = textGo.GetComponent<RectTransform>();
        if (rt == null)
        {
            rt = textGo.AddComponent<RectTransform>();
        }

        if (isNew)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = anchoredPosition;
            rt.sizeDelta = new Vector2(280f, 40f);
        }

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
        text.enabled = true;
        text.raycastTarget = false;

        Image background = textGo.GetComponent<Image>();
        if (background == null)
        {
            background = textGo.AddComponent<Image>();
        }

        background.color = new Color(0f, 0f, 0f, 0.35f);
        return text;
    }

    private static GameObject CreateCenterPanel(string name, Transform parent, string label)
    {
        GameObject panel = GameObject.Find(name);
        GameObject panelGo = panel != null ? panel : new GameObject(name);
        bool isNew = panel == null;
        panelGo.transform.SetParent(parent, false);

        RectTransform panelRt = panelGo.GetComponent<RectTransform>();
        if (panelRt == null)
        {
            panelRt = panelGo.AddComponent<RectTransform>();
        }

        if (isNew)
        {
            panelRt.anchorMin = new Vector2(0.5f, 0.5f);
            panelRt.anchorMax = new Vector2(0.5f, 0.5f);
            panelRt.pivot = new Vector2(0.5f, 0.5f);
            panelRt.sizeDelta = new Vector2(360f, 130f);
        }

        Image panelImage = panelGo.GetComponent<Image>();
        if (panelImage == null)
        {
            panelImage = panelGo.AddComponent<Image>();
        }

        panelImage.color = new Color(0f, 0f, 0f, 0.75f);

        Text labelText = CreateText($"{name}_Text", panelGo.transform, Vector2.zero, label, 44, TextAnchor.MiddleCenter);
        if (isNew)
        {
            RectTransform labelRt = labelText.rectTransform;
            labelRt.anchorMin = new Vector2(0.5f, 0.5f);
            labelRt.anchorMax = new Vector2(0.5f, 0.5f);
            labelRt.pivot = new Vector2(0.5f, 0.5f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(340f, 90f);
        }

        panelGo.SetActive(false);
        return panelGo;
    }

    private static Font GetBuiltinFont()
    {
        Font arial = Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (arial != null)
        {
            return arial;
        }

        return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
    }
}
