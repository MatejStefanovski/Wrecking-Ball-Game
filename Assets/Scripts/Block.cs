using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider2D))]
public class Block : MonoBehaviour
{
    public enum HiddenPowerupType
    {
        None = 0,
        Money = 1,
        Hammer = 2
    }

    [SerializeField] private int maxHp = 1;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color layerColor = Color.white;

    [Header("Audio")]
    [SerializeField] private AudioClip hitSfx;
    [SerializeField, Range(0f, 1f)] private float hitSfxVolume = 0.8f;
    [SerializeField] private AudioSource hitSfxSource;
    [SerializeField] private bool playHitSfxOnTakeDamage = true;

    private int currentHp;
    [Header("Powerup (optional)")]
    [SerializeField] private HiddenPowerupType hiddenPowerup = HiddenPowerupType.None;

    [Header("Overlay (default look)")]
    [SerializeField] private SpriteRenderer overlayRenderer;
    [SerializeField] private Sprite defaultOverlaySprite;

    [Header("Cracks")]
    [SerializeField] private SpriteRenderer cracksRenderer;
    [SerializeField] private Sprite lightGrayCracked;
    [SerializeField] private Sprite darkGrayCracked;
    [SerializeField] private Sprite darkGrayHeavyCracked;

    private int rowFromTop = -1;
    private Sprite lastCracksSprite;

    private static bool cachedRows;
    private static readonly List<float> rowYsDesc = new();
    private const float RowYTolerance = 0.05f;

    private void Start()
    {
        currentHp = Mathf.Max(1, maxHp);

        EnsureHitSfxSource();

        if (spriteRenderer != null)
        {
            spriteRenderer.color = layerColor;
        }

        EnsureOverlayRenderer();
        EnsureCracksRenderer();
        CacheRowsIfNeeded();
        rowFromTop = GetRowIndexFromTop(transform.position.y);
        UpdateCracksVisual();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider != null && collision.collider.CompareTag("Ball"))
        {
            TryPlayHitSfx();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other != null && other.CompareTag("Ball"))
        {
            TryPlayHitSfx();
        }
    }

    private void TryPlayHitSfx()
    {
        if (hitSfx == null)
        {
            return;
        }

        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning)
        {
            return;
        }

        EnsureHitSfxSource();
        if (hitSfxSource == null)
        {
            return;
        }

        hitSfxSource.volume = hitSfxVolume;
        hitSfxSource.PlayOneShot(hitSfx);
    }

    private void PlayHitSfxDetached()
    {
        if (hitSfx == null)
        {
            return;
        }

        GameObject sfx = new GameObject("SFX_BlockHit");
        sfx.transform.position = transform.position;

        AudioSource src = sfx.AddComponent<AudioSource>();
        src.playOnAwake = false;
        src.loop = false;
        src.spatialBlend = 0f;
        src.volume = hitSfxVolume;
        src.clip = hitSfx;
        src.Play();

        float cleanupDelay = Mathf.Max(0.1f, hitSfx.length + 0.1f);
        Destroy(sfx, cleanupDelay);
    }

    private void EnsureHitSfxSource()
    {
        if (hitSfxSource != null)
        {
            return;
        }

        hitSfxSource = GetComponent<AudioSource>();
        if (hitSfxSource == null)
        {
            hitSfxSource = gameObject.AddComponent<AudioSource>();
        }

        hitSfxSource.playOnAwake = false;
        hitSfxSource.loop = false;
        hitSfxSource.spatialBlend = 0f; // force 2D so distance/3D listener won't mute it
    }

    public void TakeDamage(int damageAmount)
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning)
        {
            return;
        }

        int appliedDamage = Mathf.Max(1, damageAmount);
        bool willDieFromThisHit = (currentHp - appliedDamage) <= 0;
        if (playHitSfxOnTakeDamage)
        {
            if (willDieFromThisHit)
            {
                PlayHitSfxDetached();
            }
            else
            {
                TryPlayHitSfx();
            }
        }

        currentHp -= appliedDamage;
        if (currentHp <= 0)
        {
            if (hiddenPowerup != HiddenPowerupType.None)
            {
                PowerupManager powerupManager = PowerupManager.Instance != null
                    ? PowerupManager.Instance
                    : FindFirstObjectByType<PowerupManager>();
                if (powerupManager != null)
                {
                    powerupManager.SpawnPowerupDrop(hiddenPowerup, transform.position);
                }
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.RegisterBlockDestroyed();
            }
            Destroy(gameObject);
            return;
        }

        UpdateCracksVisual();
    }

    public void SetHiddenPowerup(HiddenPowerupType type)
    {
        hiddenPowerup = type;
    }

    private void EnsureOverlayRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (overlayRenderer == null)
        {
            Transform existing = transform.Find("Overlay");
            if (existing != null)
            {
                overlayRenderer = existing.GetComponent<SpriteRenderer>();
            }
        }

        if (overlayRenderer == null)
        {
            GameObject overlay = new GameObject("Overlay");
            overlay.transform.SetParent(transform, false);
            overlay.transform.localPosition = Vector3.zero;
            overlayRenderer = overlay.AddComponent<SpriteRenderer>();
        }

        overlayRenderer.sprite = defaultOverlaySprite;
        overlayRenderer.enabled = defaultOverlaySprite != null;

        if (spriteRenderer != null)
        {
            overlayRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            overlayRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;
        }

        if (defaultOverlaySprite != null)
        {
            FitOverlayToBlock(defaultOverlaySprite);
        }
    }

    private void EnsureCracksRenderer()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        if (cracksRenderer != null)
        {
            return;
        }

        Transform existing = transform.Find("Cracks");
        if (existing != null)
        {
            cracksRenderer = existing.GetComponent<SpriteRenderer>();
        }

        if (cracksRenderer == null)
        {
            GameObject cracks = new GameObject("Cracks");
            cracks.transform.SetParent(transform, false);
            cracks.transform.localPosition = Vector3.zero;
            cracksRenderer = cracks.AddComponent<SpriteRenderer>();
        }

        cracksRenderer.enabled = false;
        cracksRenderer.sprite = null;

        if (spriteRenderer != null)
        {
            cracksRenderer.sortingLayerID = spriteRenderer.sortingLayerID;
            cracksRenderer.sortingOrder = spriteRenderer.sortingOrder + 2;
        }
    }

    private static void CacheRowsIfNeeded()
    {
        if (cachedRows)
        {
            return;
        }

        cachedRows = true;
        rowYsDesc.Clear();

        Block[] blocks = FindObjectsByType<Block>(FindObjectsSortMode.None);
        for (int i = 0; i < blocks.Length; i++)
        {
            float y = blocks[i].transform.position.y;
            AddRowYIfNew(y);
        }

        rowYsDesc.Sort((a, b) => b.CompareTo(a));
    }

    private static void AddRowYIfNew(float y)
    {
        for (int i = 0; i < rowYsDesc.Count; i++)
        {
            if (Mathf.Abs(rowYsDesc[i] - y) <= RowYTolerance)
            {
                return;
            }
        }
        rowYsDesc.Add(y);
    }

    private static int GetRowIndexFromTop(float y)
    {
        if (rowYsDesc.Count == 0)
        {
            return -1;
        }

        int bestIndex = 0;
        float bestDelta = float.MaxValue;
        for (int i = 0; i < rowYsDesc.Count; i++)
        {
            float delta = Mathf.Abs(rowYsDesc[i] - y);
            if (delta < bestDelta)
            {
                bestDelta = delta;
                bestIndex = i;
            }
        }
        return bestIndex;
    }

    private void UpdateCracksVisual(bool reset = false)
    {
        if (cracksRenderer == null)
        {
            return;
        }

        if (reset)
        {
            lastCracksSprite = null;
        }

        Sprite target = null;

        // Row rules (from top):
        // - Row 0 (top): when HP == 2 -> dark cracked, when HP == 1 -> dark heavy cracked
        // - Row 1 & 2: use light gray cracked once the block has been hit (HP < max)
        if (rowFromTop == 0)
        {
            if (currentHp == 2)
            {
                target = darkGrayCracked;
            }
            else if (currentHp == 1)
            {
                target = darkGrayHeavyCracked;
            }
        }
        else if (rowFromTop == 1 || rowFromTop == 2)
        {
            if (currentHp < maxHp)
            {
                target = lightGrayCracked;
            }
        }

        if (target == lastCracksSprite)
        {
            return;
        }

        lastCracksSprite = target;
        cracksRenderer.sprite = target;
        cracksRenderer.enabled = target != null;

        if (target != null)
        {
            FitCracksToBlock(target);
        }
    }

    private void FitCracksToBlock(Sprite cracksSprite)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || cracksSprite == null)
        {
            return;
        }

        // Match flips so the overlay stays aligned with the base sprite.
        cracksRenderer.flipX = spriteRenderer.flipX;
        cracksRenderer.flipY = spriteRenderer.flipY;

        Vector2 baseSize = spriteRenderer.sprite.bounds.size;
        Vector2 cracksSize = cracksSprite.bounds.size;

        if (cracksSize.x <= 0f || cracksSize.y <= 0f)
        {
            cracksRenderer.transform.localScale = Vector3.one;
            return;
        }

        cracksRenderer.transform.localPosition = Vector3.zero;
        cracksRenderer.transform.localRotation = Quaternion.identity;
        cracksRenderer.transform.localScale = new Vector3(
            baseSize.x / cracksSize.x,
            baseSize.y / cracksSize.y,
            1f
        );
    }

    private void FitOverlayToBlock(Sprite overlaySprite)
    {
        if (spriteRenderer == null || spriteRenderer.sprite == null || overlaySprite == null || overlayRenderer == null)
        {
            return;
        }

        overlayRenderer.flipX = spriteRenderer.flipX;
        overlayRenderer.flipY = spriteRenderer.flipY;

        Vector2 baseSize = spriteRenderer.sprite.bounds.size;
        Vector2 overlaySize = overlaySprite.bounds.size;
        if (overlaySize.x <= 0f || overlaySize.y <= 0f)
        {
            overlayRenderer.transform.localScale = Vector3.one;
            return;
        }

        overlayRenderer.transform.localPosition = Vector3.zero;
        overlayRenderer.transform.localRotation = Quaternion.identity;
        overlayRenderer.transform.localScale = new Vector3(
            baseSize.x / overlaySize.x,
            baseSize.y / overlaySize.y,
            1f
        );
    }
}
