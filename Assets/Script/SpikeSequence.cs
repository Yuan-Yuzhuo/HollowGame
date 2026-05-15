using System.Collections;
using UnityEngine;

public class SpikeSequence2D : MonoBehaviour
{

    public GameObject[] spikeRows;


    public float delay = 0.3f;

    private bool triggered = false;

    public void StartSequence()
    {
        if (triggered) return;

        triggered = true;

        StartCoroutine(ShowSpikes());
    }

    private IEnumerator ShowSpikes()
    {
        for (int i = 0; i < spikeRows.Length; i++)
        {
            spikeRows[i].SetActive(true);

            yield return new WaitForSeconds(delay);
        }
    }
}