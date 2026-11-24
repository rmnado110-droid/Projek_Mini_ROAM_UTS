using UnityEngine;

// Pasang di objek kosong (child dari kursi). 
// Posisi objek = tempat akar karakter berdiri saat duduk (di lantai, tepat di depan dudukan).
// Arah panah biru (Z) = arah hadap karakter saat duduk.
public class SitPoint : MonoBehaviour
{
    public float interactRange = 1.5f;      // jarak maksimal Player untuk bisa duduk
    public float standOffset = 0.8f;        // jarak karakter berdiri ke depan setelah bangun

    public Vector3 StandPosition => transform.position + transform.forward * standOffset;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.15f);
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * 0.6f);
        Gizmos.color = new Color(0, 1, 0, 0.15f);
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
