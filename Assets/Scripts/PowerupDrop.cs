using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class PowerupDrop : MonoBehaviour
{
    [SerializeField] private float fallSpeed = 4.5f;
    [SerializeField] private float despawnY = -10f;

    private Block.HiddenPowerupType powerupType;
    private PowerupManager manager;

    public void Initialize(PowerupManager owner, Block.HiddenPowerupType type, Sprite iconSprite, float speed, float despawnAtY)
    {
        manager = owner;
        powerupType = type;
        fallSpeed = Mathf.Max(0.1f, speed);
        despawnY = despawnAtY;

        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null)
        {
            sr = gameObject.AddComponent<SpriteRenderer>();
        }
        sr.sprite = iconSprite;
        sr.sortingOrder = 200;

        CircleCollider2D col = GetComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = Mathf.Max(0.05f, col.radius);

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void Update()
    {
        transform.position += Vector3.down * (fallSpeed * Time.deltaTime);
        if (transform.position.y < despawnY)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (other.GetComponent<CraneController>() == null)
        {
            return;
        }

        if (manager != null)
        {
            manager.Collect(powerupType);
        }
        Destroy(gameObject);
    }
}

