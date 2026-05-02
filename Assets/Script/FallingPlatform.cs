using UnityEngine;
using System.Collections;

public class FallingPlatform : MonoBehaviour
{
    public float delay = 0.05f;        // 稍微给点反应时间
    public float destroyTime = 0.5f;

    public float fallSpeed = 12f;       // 初始下落速度（关键）
    public float gravityScale = 85f;    // 重力加速（关键）

    private bool isTriggered = false;
    private Rigidbody2D rb;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isTriggered) return;

        if (collision.CompareTag("Player"))
        {
            isTriggered = true;
            StartCoroutine(Fall());
        }
    }

    IEnumerator Fall()
    {
        yield return new WaitForSeconds(delay);

        // 抖动提示（可以减少次数让节奏更快）
        for (int i = 0; i < 3; i++)
        {
            transform.position += Vector3.right * 0.03f;
            yield return new WaitForSeconds(0.01f);
            transform.position -= Vector3.right * 0.03f;
            yield return new WaitForSeconds(0.01f);
        }

        // 开始掉落（关键）
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = gravityScale;

        // 给一个向下初速度（核心手感）
        rb.velocity = new Vector2(0f, -fallSpeed);

        Destroy(gameObject, destroyTime);
    }
}