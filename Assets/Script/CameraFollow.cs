using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    void LateUpdate()
    {
        if (target != null)
        {
            Vector3 pos = transform.position;
            pos.x = target.position.x;
            pos.y = target.position.y;
            transform.position = pos;
        }
    }
}