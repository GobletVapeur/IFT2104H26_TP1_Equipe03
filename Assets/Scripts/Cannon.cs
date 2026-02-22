using UnityEngine;

public class Cannon : MonoBehaviour
{
    public GameObject ProjectilePrefab;
    public PlatformData Platform;

    [Header("Properties")]
    public float minSpeed = 7.5f;
    public float maxSpeed = 10.5f;

    public float spreadXZ = 0.08f;
    public Vector2 spreadYRange = new(0.40f, 0.70f);

    public Projectile CreateProjectile()
    {
        GameObject gameObject = Instantiate(ProjectilePrefab, transform.position, Quaternion.identity);
        
        Projectile projectile = gameObject.GetComponent<Projectile>();

        Vector3 target = Platform.Center;
        Vector3 direction = (target - transform.position).normalized;

        direction += new Vector3(
            Random.Range(-spreadXZ, spreadXZ),
            Random.Range(spreadYRange.x, spreadYRange.y),
            Random.Range(-spreadXZ, spreadXZ)
        );

        float speed = Random.Range(minSpeed, maxSpeed);

        projectile.Init(direction.normalized * speed, Platform);
        return projectile;
    }
}