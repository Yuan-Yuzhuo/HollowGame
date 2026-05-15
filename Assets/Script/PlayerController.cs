using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;
    public float accel = 40f;
    public float decel = 50f;
    public float airAccel = 25f;
    public float jumpForce = 14f;
    public float jumpCutMultiplier = 0.5f;
    public float fallMultiplier = 2.0f;
    public float lowJumpMultiplier = 1.4f;

    public float dashSpeed = 14f;
    public float dashTime = 0.15f;
    public float dashCooldown = 0.5f;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private float dashCooldownTimer = 0f;
    private int dashDir = 1;
    private bool canAirDash = true;

    // 二段跳
    private int jumpCount = 0;
    public int maxJumpCount = 2;

    // 接地检测
    private Rigidbody2D rb;
    private bool isGrounded = false;
    private float defaultGravityScale = 3f;

    // Coyote Time（离地容错）
    public float coyoteTime = 0.1f;
    private float coyoteTimer;

    // 攻击相关
    public Transform attackPoint;
    public float attackRange = 1f;
    public LayerMask enemyLayer;
    public int attackDamage = 1;
    public float attackKnockback = 6f;
    public GameObject attackFxPrefab;
    public float attackFxDuration = 0.2f;

    public float fallThreshold = -20f;

    // 生命与受击
    public int maxHealth = 3;
    public float invincibleTime = 0.6f;
    public float hitKnockback = 8f;
    private int currentHealth;
    private bool isInvincible = false;
    private float invincibleTimer = 0f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform visualRoot;

    private int facingDir = 1;
    private Vector3 attackPointStartLocalPos;

    // 反馈
    public ParticleSystem landParticles;
    public float landShakeDuration = 0.08f;
    public float landShakeMagnitude = 0.12f;
    public float hitShakeDuration = 0.15f;
    public float hitShakeMagnitude = 0.2f;
    public CameraFollow cameraFollow;
    public float landCooldown = 0.12f;
    private float lastLandTime = -10f;
    private HashSet<int> groundColliderIds = new HashSet<int>();
    public Transform groundCheck;
    public float groundCheckRadius = 0.12f;
    public LayerMask groundLayer;
    private bool prevGroundedState = false;
    private bool isDead = false;

    // 重生点
    public Transform respawnPoint;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
        
        animator.enabled = true;

        if (visualRoot == null && spriteRenderer != null)
            visualRoot = spriteRenderer.transform;

        if (attackPoint != null)
            attackPointStartLocalPos = attackPoint.localPosition;
    }

    void Start()
    {
        currentHealth = maxHealth;
        defaultGravityScale = rb.gravityScale;

        isDead = false;
        isDashing = false;
        isInvincible = false;

        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.enabled = true;
            animator.Play("Idle", 0, 0f);
        }
    }

    void SetFacing(int dir)
    {
        if (dir == facingDir) return;

        facingDir = dir;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = facingDir < 0;
        }

        if (attackPoint != null)
        {
            Vector3 p = attackPointStartLocalPos;
            p.x = Mathf.Abs(p.x) * facingDir;
            attackPoint.localPosition = p;
        }
    }

    void UpdateAnimParams(float move)
    {
        if (animator == null) return;


        animator.SetBool("Grounded", isGrounded);
    }

    void Update()
    {
        // 计时器
        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer -= Time.deltaTime;
        }
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            float blink = Mathf.PingPong(Time.time * 12f, 1f);
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = Mathf.Lerp(0.35f, 1f, blink);
                spriteRenderer.color = c;
            }
            if (invincibleTimer <= 0f)
            {
                isInvincible = false;
                if (spriteRenderer != null)
                {
                    Color c = spriteRenderer.color;
                    c.a = 1f;
                    spriteRenderer.color = c;
                }
            }
        }

        // 左右移动
        float move = Input.GetAxisRaw("Horizontal");
        if (animator != null)
        {
            animator.SetFloat("Speed", Mathf.Abs(move));
            animator.SetBool("Grounded", isGrounded);
        }
        animator.SetFloat("Speed", Mathf.Abs(move));
        if (Mathf.Abs(move) > 0.01f)
        {
            SetFacing(move > 0 ? 1 : -1);
        }

        UpdateAnimParams(move);
        if (!isDashing)
        {
            float targetSpeed = move * speed;
            float accelRate = isGrounded ? (Mathf.Abs(targetSpeed) > 0.01f ? accel : decel) : airAccel;
            float newSpeed = Mathf.MoveTowards(rb.velocity.x, targetSpeed, accelRate * Time.deltaTime);
            rb.velocity = new Vector2(newSpeed, rb.velocity.y);
        }

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
        if (!isDashing && Input.GetKeyDown(KeyCode.W) && (jumpCount < maxJumpCount || coyoteTimer > 0))
        {
            float currentJumpForce = jumpForce;

            // 第二次跳：力度减半
            if (jumpCount == 1)
            {
                currentJumpForce = jumpForce * 0.6f;
            }

            rb.velocity = new Vector2(rb.velocity.x, 0f);
            rb.AddForce(Vector2.up * currentJumpForce, ForceMode2D.Impulse);

            if (animator != null)
            {
                animator.SetTrigger("Jump");
            }

            jumpCount++;

            // 使用掉一次容错
            coyoteTimer = 0;
        }


        // 短按小跳
        if (Input.GetKeyUp(KeyCode.W) && rb.velocity.y > 0f)
        {
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * jumpCutMultiplier);
        }

        // 冲刺
        if (!isDashing && dashCooldownTimer <= 0f && Input.GetKeyDown(KeyCode.Space))
        {
            if (isGrounded || canAirDash)
            {
                StartDash(move);
            }
            if (animator != null)
            {
                animator.SetTrigger("Rush");
            }
        }

        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            rb.velocity = new Vector2(dashDir * dashSpeed, 0f);
            if (dashTimer <= 0f)
            {
                EndDash();
            }
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

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void Attack()
    {
        //Debug.Log("Mouse Clicked!");
        if (animator != null)
        {
            animator.SetTrigger("Attack");
        }

        if (attackFxPrefab != null && attackPoint != null)
        {
            GameObject fx = Instantiate(attackFxPrefab, attackPoint.position, Quaternion.identity);
            Vector3 scale = fx.transform.localScale;
            scale.x = facingDir >= 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            fx.transform.localScale = scale;
            Destroy(fx, attackFxDuration);
        }

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(
            attackPoint.position,
            attackRange,
            enemyLayer
        );

        foreach (Collider2D enemy in hitEnemies)
        {
            EnemyPatrol patrol = enemy.GetComponent<EnemyPatrol>();
            if (patrol != null)
            {
                Vector2 dir = (enemy.transform.position - transform.position).normalized;
                patrol.TakeDamage(attackDamage, dir * attackKnockback);
            }
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
        if (isDead) return;

        isDead = true;
        StartCoroutine(RestartAsync());
    }

    IEnumerator RestartAsync()
    {
        yield return new WaitForSeconds(0.01f);

        AsyncOperation op = SceneManager.LoadSceneAsync(
            SceneManager.GetActiveScene().buildIndex
        );

        while (!op.isDone)
        {
            yield return null;
        }
    }

    IEnumerator RestartFlow()
    {


        // 👉 异步加载当前场景
        AsyncOperation op = SceneManager.LoadSceneAsync(
            SceneManager.GetActiveScene().buildIndex
        );

        // 👉 防止场景加载到一半就切过去（更平滑）
        op.allowSceneActivation = false;

        // 👉 等加载完成（但还没激活）
        while (op.progress < 0.01f)
        {
            yield return null;
        }

        // 👉 激活新场景（真正切换）
        op.allowSceneActivation = true;
    }


    void StartDash(float inputDir)
    {
        isDashing = true;
        dashTimer = dashTime;
        dashCooldownTimer = dashCooldown;
        rb.gravityScale = 0f;
        dashDir = inputDir != 0f ? (int)Mathf.Sign(inputDir) : facingDir;

        if (!isGrounded)
        {
            canAirDash = false;
        }
    }

    void EndDash()
    {
        isDashing = false;
        rb.gravityScale = defaultGravityScale;
    }

    // 刚落地（只触发一次）
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Debug: 输出碰撞信息，便于在 Console 查看为何未触发落地反馈
#if UNITY_EDITOR
        Debug.Log($"Player OnCollisionEnter2D with {collision.gameObject.name} tag={collision.gameObject.tag} velY={rb.velocity.y} contacts={ (collision.contacts != null ? collision.contacts.Length : 0) }");
#endif

        // 记录进入时 groundColliderIds 的状态（避免重复触发）
        bool wasGroundedBefore = groundColliderIds.Count > 0;

        if (collision.gameObject.CompareTag("Ground"))
        {
            int colId = collision.collider != null ? collision.collider.GetInstanceID() : collision.gameObject.GetInstanceID();

            // 使用接触法线判断是否为脚部接触
            bool hasUpContact = false;
            if (collision.contacts != null && collision.contacts.Length > 0)
            {
                for (int i = 0; i < collision.contacts.Length; i++)
                {
                    if (collision.contacts[i].normal.y > 0.5f)
                    {
                        hasUpContact = true;
                        break;
                    }
                }
            }

            bool wasFalling = rb.velocity.y < -0.1f;

            // 仅在有上方接触时把该碰撞器计入集合（避免侧碰计入）
            if (hasUpContact && !groundColliderIds.Contains(colId))
            {
                groundColliderIds.Add(colId);
            }


            isGrounded = groundColliderIds.Count > 0;
            jumpCount = 0;
            canAirDash = true;
        }

        if (collision.gameObject.CompareTag("Enemy"))
        {
            Vector2 hitDir = (transform.position - collision.transform.position).normalized;
            TakeDamage(1, hitDir * hitKnockback);
        }
    }

    // 持续接地（防止边缘抖动）
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            int colId = collision.collider != null ? collision.collider.GetInstanceID() : collision.gameObject.GetInstanceID();
            if (!groundColliderIds.Contains(colId))
            {
                // 如果持续接触且为上方接触则计入集合
                bool hasUpContact = false;
                if (collision.contacts != null && collision.contacts.Length > 0)
                {
                    for (int i = 0; i < collision.contacts.Length; i++)
                    {
                        if (collision.contacts[i].normal.y > 0.5f)
                        {
                            hasUpContact = true;
                            break;
                        }
                    }
                }
                if (hasUpContact)
                {
                    groundColliderIds.Add(colId);
                }
            }
            isGrounded = groundColliderIds.Count > 0;
        }
    }

    // 离开地面
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            int colId = collision.collider != null ? collision.collider.GetInstanceID() : collision.gameObject.GetInstanceID();
            if (groundColliderIds.Contains(colId))
            {
                groundColliderIds.Remove(colId);
            }
            isGrounded = groundColliderIds.Count > 0;
        }
    }

    private void FixedUpdate()
    {
        if (isDashing)
        {
            return;
        }

        // 优先使用 groundCheck + OverlapCircle 来判断接地状态（更稳健），没有配置 groundCheck 则回退到 collider 集合方法
        bool nowGrounded = false;
        if (groundCheck != null)
        {
            Collider2D hit = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
            nowGrounded = hit != null;



            prevGroundedState = nowGrounded;
            isGrounded = nowGrounded;
        }
        else
        {
            // 使用碰撞器集合统计的 isGrounded（已有逻辑维护）
            // 但仍要通过 prevGroundedState 做 0->1 转换判定，避免重复触发
            nowGrounded = groundColliderIds.Count > 0;
            if (!prevGroundedState && nowGrounded && Time.time - lastLandTime > landCooldown)
            {
                lastLandTime = Time.time;
#if UNITY_EDITOR
                Debug.Log($"Player LandFeedback via collider-set triggered (prevGrounded={prevGroundedState}, groundCount={groundColliderIds.Count})");
#endif

            }
            prevGroundedState = nowGrounded;
            isGrounded = nowGrounded;
        }

        if (rb.velocity.y < 0f)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0f && !Input.GetKey(KeyCode.W))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.fixedDeltaTime;
        }
    }

    void TakeDamage(int amount, Vector2 knockback)
    {
        // 先始终应用一次击退（无论是否处于无敌）
        rb.velocity = Vector2.zero;
        rb.AddForce(knockback, ForceMode2D.Impulse);

        // 如果当前处于无敌状态，仅应用击退并返回（不再扣血）
        if (isInvincible)
        {
            return;
        }

        // 非无敌时才扣血并触发受击效果
        currentHealth -= amount;
        Debug.Log($"Player hit: remaining health = {currentHealth}");
        isInvincible = true;
        invincibleTimer = invincibleTime;

        if (animator != null)
        {
            animator.SetTrigger("Hurt");
        }
        if (cameraFollow != null)
        {
            cameraFollow.Shake(hitShakeDuration, hitShakeMagnitude);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

}