using UnityEngine;

public class EnemyPatrol : MonoBehaviour
{
    public float speed = 2f;
    private int direction = 1; // 1=右，-1=左

    // 👇 新增：边缘检测
    public Transform groundCheck;
    public float checkDistance = 0.5f;
    public LayerMask groundLayer;

    void Update()
    {
        // 移动
        transform.Translate(Vector2.right * direction * speed * Time.deltaTime);

        // 👇 检测前方是否有地面
        RaycastHit2D hit = Physics2D.Raycast(
            groundCheck.position,
            Vector2.down,
            checkDistance,
            groundLayer
        );

        // 如果前面没有地面 → 掉头
        if (!hit)
        {
            Flip();
        }
    }

    void Flip()
    {
        direction *= -1;

        // 翻转模型（让敌人朝向正确）
        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Enemy") || collision.gameObject.CompareTag("Ground"))
        {
            Flip();
        }
    }

    void OnDrawGizmos()
    {
        if (groundCheck == null) return;

        Gizmos.color = Color.green;
        Gizmos.DrawLine(
            groundCheck.position,
            groundCheck.position + Vector3.down * checkDistance
        );
    }
}