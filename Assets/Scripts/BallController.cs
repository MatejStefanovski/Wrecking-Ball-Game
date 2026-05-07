using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class BallController : MonoBehaviour
{
    [SerializeField] private int baseDamage = 1;
    [SerializeField] private SpriteRenderer ballRenderer;
    [SerializeField] private Sprite ballSprite;
    [Header("Audio")]
    [SerializeField] private AudioClip platformBounceSfx;
    [SerializeField, Range(0f, 1f)] private float platformBounceVolume = 0.8f;
    [SerializeField] private AudioSource sfxSource;
    [Header("Chain (visual only)")]
    [SerializeField] private CraneController craneController;
    [SerializeField] private LineRenderer chainRenderer;
    [SerializeField] private Sprite chainSprite;
    [SerializeField] private float chainTilesPerUnit = 6f;
    [SerializeField] private Color chainColor = Color.white;
    [SerializeField] private float chainWidth = 0.06f;
    [Header("Arcade Ball (No Physics Bounce)")]
    [SerializeField] private float ballSpeed = 11.5f;
    [SerializeField] private float minHorizontalLaunch = 1.5f;
    [SerializeField] private LayerMask collisionMask = Physics2D.DefaultRaycastLayers;
    [SerializeField] private float collisionSkin = 0.01f;

    private Transform followTarget;
    private Vector3 followOffset;
    private bool launched;
    private Vector2 moveDirection;

    private int damageMultiplier = 1;
    private Color defaultColor;
    private bool cachedDefaultColor;
    private Sprite defaultSprite;
    private bool cachedDefaultSprite;
    private Rigidbody2D rb;
    private CircleCollider2D circleCollider;
    private Collider2D leftWallCollider;
    private Collider2D rightWallCollider;
    private Collider2D topWallCollider;
    private Material chainMaterialInstance;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Discrete;

        circleCollider = GetComponent<CircleCollider2D>();
        if (circleCollider == null)
        {
            circleCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        if (collisionMask.value == 0)
        {
            collisionMask = Physics2D.DefaultRaycastLayers;
        }

        if (ballRenderer == null)
        {
            ballRenderer = GetComponent<SpriteRenderer>();
        }

        if (ballRenderer != null)
        {
            defaultColor = ballRenderer.color;
            cachedDefaultColor = true;
        }

        ApplyBallSpriteFittingCollider();
        EnsureSfxSource();
        EnsureChainRenderer();
        CacheWallColliders();
    }

    private void Update()
    {
        bool running = GameManager.Instance == null || GameManager.Instance.IsGameRunning;
        if (!running)
        {
            return;
        }

        if (!launched && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Launch();
        }

        UpdateChain();
    }

    private void FixedUpdate()
    {
        bool running = GameManager.Instance == null || GameManager.Instance.IsGameRunning;
        if (!running)
        {
            return;
        }

        if (!launched && followTarget != null)
        {
            rb.MovePosition(followTarget.position + followOffset);
            return;
        }

        CacheWallColliders();
        SimulateArcadeBall(Time.fixedDeltaTime);
    }

    public void ApplyMetalBall(float duration, Sprite metalSprite)
    {
        damageMultiplier = 2;

        if (ballRenderer != null)
        {
            if (!cachedDefaultSprite)
            {
                defaultSprite = ballRenderer.sprite;
                cachedDefaultSprite = true;
            }

            if (metalSprite != null)
            {
                ballRenderer.sprite = metalSprite;
            }
            ballRenderer.color = Color.white;
        }

        CancelInvoke(nameof(ResetMetalBallWithSprite));
        Invoke(nameof(ResetMetalBallWithSprite), duration);
    }

    private void ResetMetalBallWithSprite()
    {
        damageMultiplier = 1;

        if (ballRenderer != null)
        {
            if (cachedDefaultSprite)
            {
                ballRenderer.sprite = defaultSprite;
            }

            if (cachedDefaultColor)
            {
                ballRenderer.color = defaultColor;
            }
        }
    }

    public void ConfigureLaunch(Transform platform, Vector3 offset)
    {
        followTarget = platform;
        followOffset = offset;
        launched = false;
        moveDirection = Vector2.up;
    }

    private void Launch()
    {
        launched = true;

        float horizontal = Random.Range(-1f, 1f);
        if (Mathf.Abs(horizontal) < 0.25f)
        {
            horizontal = Mathf.Sign(horizontal == 0f ? 1f : horizontal) * 0.25f;
        }

        Vector2 direction = new Vector2(horizontal * minHorizontalLaunch, 1f).normalized;
        moveDirection = direction;
    }

    private void SimulateArcadeBall(float dt)
    {
        float remainingDistance = ballSpeed * dt;
        int bounceLimit = 8;
        float radius = circleCollider.radius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y));
        Vector2 currentPosition = rb.position;

        while (remainingDistance > 0f && bounceLimit-- > 0)
        {
            RaycastHit2D hit = GetNearestSolidHit(currentPosition, radius, moveDirection, remainingDistance + collisionSkin);
            if (hit.collider == null)
            {
                currentPosition += moveDirection * remainingDistance;
                remainingDistance = 0f;
                break;
            }

            float travel = Mathf.Max(0f, hit.distance - collisionSkin);
            if (travel > 0f)
            {
                currentPosition += moveDirection * travel;
                remainingDistance -= travel;
            }

            HandleCollision(hit, currentPosition);
            currentPosition += moveDirection * collisionSkin;
            remainingDistance -= collisionSkin;
        }

        // Keep the ball inside the playable box even in squeeze edge-cases.
        ConstrainInsideWalls(ref currentPosition, radius);

        rb.MovePosition(currentPosition);
    }

    private void ApplyBallSpriteFittingCollider()
    {
        if (ballRenderer == null || ballSprite == null || circleCollider == null)
        {
            return;
        }

        ballRenderer.sprite = ballSprite;
        ballRenderer.color = Color.white;

        // Keep the GameObject scale unchanged; fit the sprite to the collider diameter.
        // (Margins you see are usually transparent padding inside the PNG; this makes the
        // sprite fill the physics ball as much as possible without changing physics size.)
        ballRenderer.drawMode = SpriteDrawMode.Sliced;
        float diameterLocal = Mathf.Max(0.0001f, circleCollider.radius * 2f);
        ballRenderer.size = new Vector2(diameterLocal, diameterLocal);

        // Cache the "default" color AFTER applying the new sprite, so metal-ball reset
        // goes back to the right brightness.
        defaultColor = ballRenderer.color;
        cachedDefaultColor = true;
    }

    private void EnsureChainRenderer()
    {
        if (craneController == null)
        {
            craneController = FindFirstObjectByType<CraneController>();
        }

        if (chainRenderer != null)
        {
            ConfigureChainRenderer(chainRenderer);
            chainRenderer.enabled = false;
            return;
        }

        GameObject chain = new GameObject("Chain");
        chain.transform.SetParent(transform, false);
        chainRenderer = chain.AddComponent<LineRenderer>();
        ConfigureChainRenderer(chainRenderer);
        chainRenderer.enabled = false;
    }

    private void ConfigureChainRenderer(LineRenderer lr)
    {
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = chainWidth;
        lr.endWidth = chainWidth;
        lr.numCapVertices = 3;
        lr.startColor = chainColor;
        lr.endColor = chainColor;
        lr.textureMode = LineTextureMode.Tile;
        lr.alignment = LineAlignment.View;

        EnsureChainMaterial(lr);

        if (ballRenderer != null)
        {
            lr.sortingLayerID = ballRenderer.sortingLayerID;
            lr.sortingOrder = ballRenderer.sortingOrder - 1;
        }
    }

    private void EnsureChainMaterial(LineRenderer lr)
    {
        if (chainMaterialInstance == null)
        {
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                chainMaterialInstance = new Material(shader);
            }
        }

        if (chainMaterialInstance == null)
        {
            return;
        }

        if (chainSprite != null)
        {
            Texture2D tex = chainSprite.texture;
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Repeat;
                chainMaterialInstance.mainTexture = tex;
            }
        }

        lr.material = chainMaterialInstance;
    }

    private void UpdateChain()
    {
        if (chainRenderer == null || craneController == null)
        {
            return;
        }

        chainRenderer.enabled = true;

        Vector3 start = GetPlatformCenterWorld(craneController);
        Vector3 end = ballRenderer != null ? ballRenderer.bounds.center : transform.position;
        chainRenderer.SetPosition(0, start);
        chainRenderer.SetPosition(1, end);

        if (chainRenderer.material != null)
        {
            float length = Vector3.Distance(start, end);
            chainRenderer.material.mainTextureScale = new Vector2(length * Mathf.Max(0.01f, chainTilesPerUnit), 1f);
        }
    }

    private static Vector3 GetPlatformCenterWorld(CraneController crane)
    {
        if (crane == null)
        {
            return Vector3.zero;
        }

        Collider2D col = crane.GetComponent<Collider2D>();
        if (col != null)
        {
            return col.bounds.center;
        }

        SpriteRenderer sr = crane.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            return sr.bounds.center;
        }

        return crane.transform.position;
    }

    private RaycastHit2D GetNearestSolidHit(Vector2 origin, float radius, Vector2 direction, float distance)
    {
        RaycastHit2D[] hits = Physics2D.CircleCastAll(origin, radius, direction, distance, collisionMask);
        if (hits == null || hits.Length == 0)
        {
            return default;
        }

        RaycastHit2D nearest = default;
        float nearestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit2D hit = hits[i];
            if (hit.collider == null || hit.collider.isTrigger || hit.collider == circleCollider)
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearest = hit;
                nearestDistance = hit.distance;
            }
        }

        return nearest;
    }

    private void HandleCollision(RaycastHit2D hit, Vector2 currentPosition)
    {
        Block block = hit.collider.GetComponent<Block>();
        if (block != null)
        {
            int totalDamage = Mathf.Max(1, baseDamage * damageMultiplier);
            block.TakeDamage(totalDamage);
        }

        moveDirection = Vector2.Reflect(moveDirection, hit.normal).normalized;

        CraneController crane = hit.collider.GetComponent<CraneController>();
        if (crane != null)
        {
            PlayPlatformBounceSfx();
            float halfWidth = hit.collider.bounds.extents.x;
            float offset = Mathf.Clamp((currentPosition.x - crane.transform.position.x) / Mathf.Max(0.01f, halfWidth), -1f, 1f);
            moveDirection = new Vector2(offset, Mathf.Abs(moveDirection.y)).normalized;
        }

        if (Mathf.Abs(moveDirection.y) < 0.2f)
        {
            moveDirection = new Vector2(moveDirection.x, Mathf.Sign(moveDirection.y == 0f ? 1f : moveDirection.y) * 0.2f).normalized;
        }
    }

    private void EnsureSfxSource()
    {
        if (sfxSource != null)
        {
            return;
        }

        sfxSource = GetComponent<AudioSource>();
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
        }

        sfxSource.playOnAwake = false;
        sfxSource.loop = false;
        sfxSource.spatialBlend = 0f;
    }

    private void PlayPlatformBounceSfx()
    {
        if (platformBounceSfx == null || platformBounceVolume <= 0f)
        {
            return;
        }

        EnsureSfxSource();
        if (sfxSource == null)
        {
            return;
        }

        sfxSource.volume = platformBounceVolume;
        sfxSource.PlayOneShot(platformBounceSfx);
    }

    private void CacheWallColliders()
    {
        if (leftWallCollider == null)
        {
            GameObject left = GameObject.Find("LeftWall");
            if (left != null)
            {
                leftWallCollider = left.GetComponent<Collider2D>();
            }
        }

        if (rightWallCollider == null)
        {
            GameObject right = GameObject.Find("RightWall");
            if (right != null)
            {
                rightWallCollider = right.GetComponent<Collider2D>();
            }
        }

        if (topWallCollider == null)
        {
            GameObject top = GameObject.Find("TopWall");
            if (top != null)
            {
                topWallCollider = top.GetComponent<Collider2D>();
            }
        }
    }

    private void ConstrainInsideWalls(ref Vector2 position, float radius)
    {
        if (leftWallCollider != null)
        {
            float minX = leftWallCollider.bounds.max.x + radius + collisionSkin;
            if (position.x < minX)
            {
                position.x = minX;
                moveDirection.x = Mathf.Abs(moveDirection.x);
                moveDirection = moveDirection.normalized;
            }
        }

        if (rightWallCollider != null)
        {
            float maxX = rightWallCollider.bounds.min.x - radius - collisionSkin;
            if (position.x > maxX)
            {
                position.x = maxX;
                moveDirection.x = -Mathf.Abs(moveDirection.x);
                moveDirection = moveDirection.normalized;
            }
        }

        if (topWallCollider != null)
        {
            float maxY = topWallCollider.bounds.min.y - radius - collisionSkin;
            if (position.y > maxY)
            {
                position.y = maxY;
                moveDirection.y = -Mathf.Abs(moveDirection.y);
                moveDirection = moveDirection.normalized;
            }
        }
    }
}
