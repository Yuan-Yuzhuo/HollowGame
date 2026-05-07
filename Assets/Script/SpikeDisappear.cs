using System.Collections;
using UnityEngine;

public class SpikeDisappear : MonoBehaviour
{
    public GameObject spikeGroup;   // 拖入 SpikeGroup
    public float delayTime = 6.5f;  // 6.5秒后触发
    public float hideTime = 0.5f;   // 消失0.5秒

    void Start()
    {
        StartCoroutine(DisappearRoutine());
    }

    IEnumerator DisappearRoutine()
    {
        // 等待6.5秒
        yield return new WaitForSeconds(delayTime);

        // 隐藏地刺
        spikeGroup.SetActive(false);

        // 等待0.5秒
        yield return new WaitForSeconds(hideTime);

        // 重新出现
        spikeGroup.SetActive(true);
    }
}