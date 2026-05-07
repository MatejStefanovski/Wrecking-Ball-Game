using UnityEngine;
using System.Collections.Generic;

public class PowerupManager : MonoBehaviour
{
    public static PowerupManager Instance { get; private set; }

    [SerializeField] private CraneController craneController;
    [SerializeField] private BallController ballController;
    [Header("Powerup settings")]
    [SerializeField] private float powerupDuration = 5f;
    [SerializeField] private float dropFallSpeed = 4.5f;
    [SerializeField] private float dropDespawnPadding = 2f;

    [Header("Powerup icons (falling pickups)")]
    [SerializeField] private Sprite moneyIcon;
    [SerializeField] private Sprite hammerIcon;

    [Header("Money powerup (platform)")]
    [SerializeField] private Sprite upgradedPlatformSprite;
    [SerializeField, Range(1.0f, 2.0f)] private float platformWidthMultiplier = 1.2f; // +10% left and right => +20% total

    [Header("Hammer powerup (ball)")]
    [SerializeField] private Sprite metalBallSprite;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSfx;
    [SerializeField, Range(0f, 1f)] private float pickupSfxVolume = 0.9f;

    private bool assignedHiddenPowerups;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (craneController == null)
        {
            craneController = FindFirstObjectByType<CraneController>();
        }

        if (ballController == null)
        {
            ballController = FindFirstObjectByType<BallController>();
        }
    }

    private void Start()
    {
        AssignHiddenPowerupsIfNeeded();
    }

    public void ActivateMoneyUpgrade()
    {
        if (!GameManager.Instance.IsGameRunning || craneController == null)
        {
            return;
        }

        craneController.ApplyPlatformUpgrade(powerupDuration, platformWidthMultiplier, upgradedPlatformSprite);
    }

    public void ActivateMetalBall()
    {
        if (!GameManager.Instance.IsGameRunning || ballController == null)
        {
            return;
        }

        ballController.ApplyMetalBall(powerupDuration, metalBallSprite);
    }

    public void Collect(Block.HiddenPowerupType type)
    {
        if (GameManager.Instance == null || !GameManager.Instance.IsGameRunning)
        {
            return;
        }

        bool collected = false;
        switch (type)
        {
            case Block.HiddenPowerupType.Money:
                ActivateMoneyUpgrade();
                collected = true;
                break;
            case Block.HiddenPowerupType.Hammer:
                ActivateMetalBall();
                collected = true;
                break;
        }

        if (collected)
        {
            PlaySfxDetached(pickupSfx, pickupSfxVolume);
        }
    }

    public void SpawnPowerupDrop(Block.HiddenPowerupType type, Vector3 worldPosition)
    {
        if (type == Block.HiddenPowerupType.None)
        {
            return;
        }

        Sprite icon = type == Block.HiddenPowerupType.Money ? moneyIcon : hammerIcon;
        if (icon == null)
        {
            return;
        }

        float despawnY = -10f;
        if (GameManager.Instance != null)
        {
            despawnY = GameManager.Instance.OutOfBoundsY - Mathf.Max(0f, dropDespawnPadding);
        }

        GameObject go = new GameObject($"PowerupDrop_{type}");
        go.transform.position = worldPosition;
        PowerupDrop drop = go.AddComponent<PowerupDrop>();
        drop.Initialize(this, type, icon, dropFallSpeed, despawnY);
    }

    private void AssignHiddenPowerupsIfNeeded()
    {
        if (assignedHiddenPowerups)
        {
            return;
        }

        Block[] blocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        if (blocks == null || blocks.Length < 2)
        {
            return;
        }

        List<Block> candidates = new List<Block>(blocks.Length);
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] != null)
            {
                candidates.Add(blocks[i]);
            }
        }
        if (candidates.Count < 2)
        {
            return;
        }

        int moneyIndex = Random.Range(0, candidates.Count);
        int hammerIndex = moneyIndex;
        int guard = 0;
        while (hammerIndex == moneyIndex && guard++ < 20)
        {
            hammerIndex = Random.Range(0, candidates.Count);
        }
        if (hammerIndex == moneyIndex)
        {
            hammerIndex = (moneyIndex + 1) % candidates.Count;
        }

        candidates[moneyIndex].SetHiddenPowerup(Block.HiddenPowerupType.Money);
        candidates[hammerIndex].SetHiddenPowerup(Block.HiddenPowerupType.Hammer);
        assignedHiddenPowerups = true;
    }

    private static void PlaySfxDetached(AudioClip clip, float volume)
    {
        if (clip == null || volume <= 0f)
        {
            return;
        }

        GameObject sfx = new GameObject("SFX_PowerupPickup");
        AudioSource src = sfx.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.volume = Mathf.Clamp01(volume);
        src.clip = clip;
        src.Play();

        float cleanupDelay = Mathf.Max(0.1f, clip.length + 0.1f);
        Destroy(sfx, cleanupDelay);
    }
}
