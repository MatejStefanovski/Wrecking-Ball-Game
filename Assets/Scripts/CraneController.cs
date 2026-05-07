using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class CraneController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private SpriteRenderer craneRenderer;
    [SerializeField] private Sprite platformSprite;
    [SerializeField] private float xMin = -7.5f;
    [SerializeField] private float xMax = 1.5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Vector3 defaultScale;
    private bool defaultsCached;
    private BoxCollider2D boxCollider;
    private Sprite defaultSprite;
    private bool spriteDefaultCached;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionY;

        boxCollider = GetComponent<BoxCollider2D>();
        if (craneRenderer == null)
        {
            craneRenderer = GetComponent<SpriteRenderer>();
        }

        ApplyPlatformSprite();

        if (craneRenderer != null)
        {
            CacheDefaults();
        }
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameRunning)
        {
            moveInput = Vector2.zero;
            return;
        }

        float horizontal = 0f;
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            moveInput = Vector2.zero;
            return;
        }

        if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed)
        {
            horizontal = -1f;
        }
        else if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed)
        {
            horizontal = 1f;
        }

        moveInput = new Vector2(horizontal, 0f);
    }

    private void FixedUpdate()
    {
        Vector2 targetVelocity = new Vector2(moveInput.x * moveSpeed, 0f);
        rb.linearVelocity = targetVelocity;

        Vector2 clampedPosition = rb.position;
        clampedPosition.x = Mathf.Clamp(clampedPosition.x, xMin, xMax);
        rb.position = clampedPosition;
    }

    public void ApplyPlatformUpgrade(float duration, float widthMultiplier, Sprite upgradedSprite)
    {
        CacheDefaults();

        float safeMultiplier = Mathf.Max(1f, widthMultiplier);
        transform.localScale = new Vector3(defaultScale.x * safeMultiplier, defaultScale.y, defaultScale.z);

        if (craneRenderer != null)
        {
            if (!spriteDefaultCached)
            {
                defaultSprite = craneRenderer.sprite;
                spriteDefaultCached = true;
            }

            if (upgradedSprite != null)
            {
                craneRenderer.sprite = upgradedSprite;
            }
        }

        CancelInvoke(nameof(ResetPlatformUpgrade));
        Invoke(nameof(ResetPlatformUpgrade), duration);
    }

    private void ResetPlatformUpgrade()
    {
        if (!defaultsCached)
        {
            return;
        }

        transform.localScale = defaultScale;

        if (craneRenderer != null && spriteDefaultCached)
        {
            craneRenderer.sprite = defaultSprite;
        }
    }

    private void CacheDefaults()
    {
        if (defaultsCached)
        {
            return;
        }

        defaultScale = transform.localScale;

        defaultsCached = true;
    }

    private void ApplyPlatformSprite()
    {
        if (craneRenderer == null)
        {
            return;
        }

        if (platformSprite != null)
        {
            craneRenderer.sprite = platformSprite;
        }

        FitSpriteToObjectSize();
    }

    private void FitSpriteToObjectSize()
    {
        if (craneRenderer == null || craneRenderer.sprite == null || boxCollider == null)
        {
            return;
        }

        // Keep the GameObject's scale/width unchanged.
        // Instead, resize the SpriteRenderer to match the collider (i.e. the object size).
        craneRenderer.drawMode = SpriteDrawMode.Sliced;
        craneRenderer.size = boxCollider.size;
    }
}
