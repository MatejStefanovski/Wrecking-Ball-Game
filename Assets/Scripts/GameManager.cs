using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Gameplay")]
    [SerializeField] private float baseStartTimeSeconds = 60f;
    [SerializeField] private float minimumTimeSeconds = 15f;
    [SerializeField] private float timeDecreasePerWin = 5f;

    [SerializeField] private float ballSpeedMultiplierPerWin = 1.2f;

    [SerializeField] private float outOfBoundsY = -8f;

    [SerializeField] private BallController ballController;
    [SerializeField] private UIManager uiManager;

    [Header("Scenes")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Audio")]
    [SerializeField] private AudioClip winSfx;
    [SerializeField] private AudioClip loseSfx;

    [SerializeField, Range(0f, 1f)]
    private float endSfxVolume = 0.9f;

    [Header("Background")]
    [SerializeField] private Sprite backgroundSprite;
    [SerializeField] private int backgroundSortingOrder = -100;

    // STATIC = survives scene reload
    private static int wonLevels = 0;
    private static float savedBallSpeed = -1f;

    private float currentTime;
    private int blocksRemaining;

    private bool gameEnded;
    private bool gameStarted;
    private bool playerWon;

    private float currentStartTime;

    public bool IsGameRunning => gameStarted && !gameEnded;
    public float OutOfBoundsY => outOfBoundsY;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (ballController == null)
        {
            ballController = FindAnyObjectByType<BallController>();
        }

        if (uiManager == null)
        {
            uiManager = FindAnyObjectByType<UIManager>();
        }

        // FIRST START ONLY
        if (savedBallSpeed < 0f && ballController != null)
        {
            savedBallSpeed = ballController.BallSpeed;
        }

        // Calculate reduced time
        currentStartTime =
            Mathf.Max(
                minimumTimeSeconds,
                baseStartTimeSeconds - (wonLevels * timeDecreasePerWin)
            );

        // Apply saved speed
        if (ballController != null)
        {
            ballController.BallSpeed = savedBallSpeed;
        }

        currentTime = currentStartTime;

        blocksRemaining =
            FindObjectsByType<Block>(FindObjectsSortMode.None).Length;

        if (uiManager != null)
        {
            uiManager.UpdateTimer(currentTime);
            uiManager.UpdateBlocksRemaining(blocksRemaining);
            uiManager.UpdateLevel(wonLevels);
            uiManager.HideEndScreens();
        }

        SpawnBackground();

        Debug.Log("LEVELS WON: " + wonLevels);
        Debug.Log("CURRENT TIME LIMIT: " + currentStartTime);
        Debug.Log("CURRENT BALL SPEED: " + savedBallSpeed);
    }

    private void Update()
    {
        // ESC -> Main Menu
        if (Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ResetProgress();
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        // WIN -> harder next level
        if (gameEnded && playerWon)
        {
            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                wonLevels++;

                // increase speed permanently
                savedBallSpeed *= ballSpeedMultiplierPerWin;

                SceneManager.LoadScene(
                    SceneManager.GetActiveScene().buildIndex
                );
            }

            return;
        }

        // LOSE -> restart from beginning
        if (gameEnded)
        {
            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                ResetProgress();

                SceneManager.LoadScene(
                    SceneManager.GetActiveScene().buildIndex
                );
            }

            return;
        }

        // Debug instant win
        if (Keyboard.current != null &&
            Keyboard.current.zKey.wasPressedThisFrame)
        {
            gameStarted = true;
            EndGame(true);
            return;
        }

        // Start game
        if (!gameStarted)
        {
            if (Keyboard.current != null &&
                Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                gameStarted = true;
            }

            return;
        }

        // Timer
        currentTime -= Time.deltaTime;

        if (uiManager != null)
        {
            uiManager.UpdateTimer(Mathf.Max(0f, currentTime));
        }

        // Lose on timeout
        if (currentTime <= 0f)
        {
            EndGame(false);
            return;
        }

        // Lose if ball falls
        if (ballController != null &&
            ballController.transform.position.y < outOfBoundsY)
        {
            EndGame(false);
        }
    }

    private void ResetProgress()
    {
        wonLevels = 0;

        if (ballController != null)
        {
            savedBallSpeed = 11.5f;
        }
    }

    public void RegisterBlockDestroyed()
    {
        if (gameEnded)
        {
            return;
        }

        blocksRemaining = Mathf.Max(0, blocksRemaining - 1);

        if (uiManager != null)
        {
            uiManager.UpdateBlocksRemaining(blocksRemaining);
        }

        AddTime(2f);

        if (blocksRemaining == 0)
        {
            EndGame(true);
        }
    }

    public void AddTime(float seconds)
    {
        if (gameEnded)
        {
            return;
        }

        currentTime += seconds;

        if (uiManager != null)
        {
            uiManager.UpdateTimer(Mathf.Max(0f, currentTime));
        }
    }

    public void EndGame(bool win)
    {
        if (gameEnded)
        {
            return;
        }

        gameEnded = true;
        playerWon = win;

        AudioClip clip = win ? winSfx : loseSfx;
        PlaySfxDetached(clip, endSfxVolume);

        if (uiManager != null)
        {
            uiManager.ShowEndScreen(win);
        }
    }

    private void SpawnBackground()
    {
        if (backgroundSprite == null)
        {
            return;
        }

        Camera cam = Camera.main;

        if (cam == null || !cam.orthographic)
        {
            return;
        }

        GameObject bg = new GameObject("Background");

        SpriteRenderer sr = bg.AddComponent<SpriteRenderer>();
        sr.sprite = backgroundSprite;
        sr.sortingOrder = backgroundSortingOrder;

        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;

        Vector2 spriteSize = backgroundSprite.bounds.size;

        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        float scale = Mathf.Max(
            worldWidth / spriteSize.x,
            worldHeight / spriteSize.y
        );

        bg.transform.position = new Vector3(
            cam.transform.position.x,
            cam.transform.position.y,
            0f
        );

        bg.transform.localScale =
            new Vector3(scale, scale, 1f);
    }

    private static void PlaySfxDetached(
        AudioClip clip,
        float volume)
    {
        if (clip == null || volume <= 0f)
        {
            return;
        }

        GameObject sfx = new GameObject("SFX_GameEnd");

        AudioSource src = sfx.AddComponent<AudioSource>();

        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.volume = Mathf.Clamp01(volume);
        src.clip = clip;

        src.Play();

        float cleanupDelay =
            Mathf.Max(0.1f, clip.length + 0.1f);

        Destroy(sfx, cleanupDelay);
    }
}