using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float jumpForce = 7f;

    // 二段跳
    private int jumpCount = 0;
    public int maxJumpCount = 2;

    // 接地检测
    private Rigidbody2D rb;
    private bool isGrounded = false;

    // Coyote Time（离地容错）
    public float coyoteTime = 0.1f;
    private float coyoteTimer;

    // 攻击相关
    public Transform attackPoint;
    public float attackRange = 1f;
    public LayerMask enemyLayer;

    public float fallThreshold = -20f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // 左右移动
        float move = Input.GetAxis("Horizontal");
        rb.velocity = new Vector2(move * speed, rb.velocity.y);

        // Coyote Time 计时
        if (isGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        // 跳跃逻辑
        if (Input.GetKeyDown(KeyCode.Space) && (jumpCount < maxJumpCount || coyoteTimer > 0))
        {
            float currentJumpForce = jumpForce;

            // 第二次跳：力度减半
            if (jumpCount == 1)
            {
                currentJumpForce = jumpForce * 0.6f;
            }

            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * currentJumpForce, ForceMode2D.Impulse);

            jumpCount++;

            // 使用掉一次容错
            coyoteTimer = 0;
        }


        if (Input.GetMouseButtonDown(0))
        {
            Attack();
        }

        // 掉落死亡检测
        if (transform.position.y < fallThreshold)
        {
            Die();
        }

    }

    void Attack()
    {
        //Debug.Log("Mouse Clicked!");
        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            Destroy(enemy.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackPoint.position, attackRange);
    }

    void Die()
    {
        // 重新加载当前场景
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
        );
    }

    // 刚落地（只触发一次）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
            jumpCount = 0;
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            if (rb.velocity.y < 0 && transform.position.y > collision.transform.position.y)
            {
                Destroy(collision.gameObject);

                rb.velocity = new Vector2(rb.velocity.x, 0f);
                rb.AddForce(Vector2.up * jumpForce * 0.7f, ForceMode2D.Impulse);
            }
            else
            {
                Die();
            }
        }
    }

    // 持续接地（防止边缘抖动）
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = true;
        }
    }

    // 离开地面
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            isGrounded = false;
        }
    }
}