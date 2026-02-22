using UnityEngine;


public class PlatformData : MonoBehaviour
{
    // AABB
    public Vector3 Size = new Vector3(40f, 1f, 50f);

    public Vector3 Center => transform.position;
    public Vector3 HalfExtents => Size * 0.5f;

    public Vector3 Min => Center - HalfExtents;
    public Vector3 Max => Center + HalfExtents;
}