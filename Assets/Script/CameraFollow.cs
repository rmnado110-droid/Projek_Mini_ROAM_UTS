using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector3 offset = new Vector3(0, 3f, -2f);
    public float smoothSpeed = 8f;
    public float lookHeight = 1.2f;
    public LayerMask obstacleMask = ~0;   // semua layer dianggap penghalang
    public float wallPadding = 0.3f;      // jarak aman dari dinding

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 focus = target.position + Vector3.up * lookHeight;
        Vector3 desiredPos = target.position + offset;
        Vector3 dir = desiredPos - focus;

        // tembak garis dari Player ke posisi kamera, kalau kena dinding, kamera dimajukan
        if (Physics.Raycast(focus, dir.normalized, out RaycastHit hit, dir.magnitude, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (!hit.transform.IsChildOf(target))
                desiredPos = hit.point - dir.normalized * wallPadding;
        }

        transform.position = Vector3.Lerp(transform.position, desiredPos, smoothSpeed * Time.deltaTime);
        transform.LookAt(focus);
    }
}