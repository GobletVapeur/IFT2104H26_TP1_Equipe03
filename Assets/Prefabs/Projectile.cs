using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Linear motion")]
    public Vector3 linearVelocity;
    public Vector3 gravityAcceleration = new Vector3(0f, -9.81f, 0f);

    [Header("Physical caracteristics")]
    public float sphereRadius = 0.5f;
    public float sphereMass = 1.0f;
    
    
    [Header("Lifetime")]
    public float maxLifetimeSeconds = 6f;

    [Header("Restitution")]
    public float minVerticalBounceSpeed = 0.5f;
    // Bounce Coefficient (Will lose energy after impact)
    [Range(0f, 1f)] public float platformRestitution = 0.7f;
    [Range(0f, 1f)] public float sphereRestitution = 0.8f;
    
    [Header("Friction")]
    // Simplified 
    public float platformFrictionStrength = 6.0f;
    public float sphereFrictionStrength = 2.0f;

    [Header("Rotation")]
    // L
    public Vector3 angularMomentum;  
    
    private float livedTimeSeconds = 0f;
    private bool isDestroyed = false;

    private PlatformData platformData;
    private float destroyBelowY;

    // Sphere inertia: I = 2/5 * m * r^2
    float sphereInertia => 0.4f * sphereMass * sphereRadius * sphereRadius;

    // w = L / I (rad/s)
    Vector3 angularVelocityRad => (sphereInertia > 1e-8f) ? (angularMomentum / sphereInertia) : Vector3.zero;

    public void Init(Vector3 initialVelocity, PlatformData platform)
    {
        linearVelocity = initialVelocity;
        platformData = platform;
        
        Vector3 initialAngularVelocityRad = new Vector3(
            Random.Range(-12f, 12f),
            Random.Range(-20f, 20f),
            Random.Range(-12f, 12f)
        );
        
        // L = w * I
        angularMomentum = initialAngularVelocityRad * sphereInertia;
    }

    void Update()
    {
        if (isDestroyed) return;

        float dt = Time.deltaTime;

        // v = v + a*dt ; x = x + v*dt
        linearVelocity += gravityAcceleration * dt;
        transform.position += linearVelocity * dt;

        // Rotation
        IntegrateRotationFromAngularMomentum(dt);

        // Collisions 
        ResolveSphereSphereCollisions(dt);
        ResolveSpherePlatformCollision(dt);

        // Destroy phase
        livedTimeSeconds += dt;
        if (transform.position.y < destroyBelowY || livedTimeSeconds > maxLifetimeSeconds)
            DestroySelf();
    }

    void IntegrateRotationFromAngularMomentum(float dt) {

        Vector3 omega = angularVelocityRad; // w
        float omegaScalar = omega.magnitude;
        if (omegaScalar < 1e-6f) return;

        // Angle performed in that time window
        float angleDegrees = omegaScalar * dt * Mathf.Rad2Deg;
        Vector3 rotationAxis = omega / omegaScalar;

        transform.rotation = Quaternion.AngleAxis(angleDegrees, rotationAxis) * transform.rotation;
    }

    void ResolveSpherePlatformCollision(float dt)
    {
        Vector3 sphereCenter = transform.position;
        Vector3 platformCenter = platformData.Center;
        Vector3 platformHalfExtents = platformData.HalfExtents; // AABB

        float maxReach = platformHalfExtents.magnitude + sphereRadius + 2f;
        
        // No collisions
        if ((sphereCenter - platformCenter).sqrMagnitude > maxReach * maxReach || linearVelocity.y >= 0f || !SphereIntersectsAabb(sphereCenter, sphereRadius, platformData.Min, platformData.Max))
            return;
        
        Vector3 planeNormal = Vector3.up;

        // Prevents jitter
        sphereCenter.y = platformData.Max.y + sphereRadius + 0.001f;
        transform.position = sphereCenter;
        
        ApplyNormalBounceAgainstStaticPlane(planeNormal, platformRestitution);
        ApplyFrictionAgainstStaticPlane(planeNormal, platformFrictionStrength, dt);

        // Prevents Micro-dribbles
        if (Mathf.Abs(linearVelocity.y) < minVerticalBounceSpeed)
            linearVelocity.y = 0f;
    }

    void ApplyNormalBounceAgainstStaticPlane(Vector3 normalVector3, float restitution)
    {
        float normalSpeed = Vector3.Dot(linearVelocity, normalVector3);
        if (normalSpeed >= 0f) return; // Goes away

        // j = -(1 + e) * normalSpeed * m
        float impulseMagnitude = -(1f + restitution) * normalSpeed * sphereMass;
        Vector3 impulse = impulseMagnitude * normalVector3;

        // delta_velocity = J / m
        linearVelocity += impulse / sphereMass;
    }

    void ApplyFrictionAgainstStaticPlane(Vector3 normalPlan, float frictionStrength, float dt)
    {
        Vector3 contactOffset = -normalPlan * sphereRadius;
        Vector3 omega = angularVelocityRad; // w
        Vector3 atPointOfContactVelocity = linearVelocity + Vector3.Cross(omega, contactOffset);

        // Flat Speed
        Vector3 tangentVelocity = atPointOfContactVelocity - Vector3.Dot(atPointOfContactVelocity, normalPlan) * normalPlan;
        if (tangentVelocity.sqrMagnitude < 1e-10f) return;

        // J = -k * vT * dt * m
        Vector3 tangentialImpulse = (-frictionStrength * dt * sphereMass) * tangentVelocity;
        
        linearVelocity += tangentialImpulse / sphereMass;

        // Lose momentum
        angularMomentum += Vector3.Cross(contactOffset, tangentialImpulse);
    }
    
    void ResolveSphereSphereCollisions(float dt)
    {
        Projectile[] projectiles = GameManager.Instance.GetProjectilesSnapshot();
        Vector3 thisCenter = transform.position;

        for (int i = 0; i < projectiles.Length; i++)
        {
            Projectile other = projectiles[i];

            // Prevents double treatment. Is there a better way?
            if (GetInstanceID() > other.GetInstanceID()) continue;

            Vector3 otherCenter = other.transform.position;
            float combinedRadius = sphereRadius + other.sphereRadius;
            Vector3 delta = otherCenter - thisCenter;

            // No collision
            if (delta.sqrMagnitude > combinedRadius * combinedRadius)
                continue;

            float distance = Mathf.Sqrt(Mathf.Max(delta.sqrMagnitude, 1e-8f));
            Vector3 collisionNormal = delta / distance;
            
            // Uncloak both
            float insideDepth = combinedRadius - distance;
            Vector3 separation = collisionNormal * (insideDepth * 0.5f + 0.0004f);
            transform.position -= separation;
            other.transform.position += separation;
            
            ApplySphereSphereNormalImpulse(other, collisionNormal);
            ApplySphereSphereFriction(other, collisionNormal, dt);
        }
    }

    void ApplySphereSphereNormalImpulse(Projectile other, Vector3 collisionNormal)
    {
        float relativeSpeedAlongNormal = Vector3.Dot(linearVelocity - other.linearVelocity, collisionNormal);
        if (relativeSpeedAlongNormal <= 0f) return;

        float e = Mathf.Min(sphereRestitution, other.sphereRestitution);
        
        // j = (1+e)*vRel / (1/m1 + 1/m2)
        float impulseMagnitude = (1f + e) * relativeSpeedAlongNormal / (1f / sphereMass + 1f / other.sphereMass);
        Vector3 impulse = impulseMagnitude * collisionNormal;

        linearVelocity += - impulse / sphereMass;
        other.linearVelocity += impulse / other.sphereMass;
    }

    void ApplySphereSphereFriction(
        Projectile otherProjectile,
        Vector3 collisionNormalDirection,
        float deltaTime)
    {
        float frictionCoefficient =
            Mathf.Min(this.sphereFrictionStrength, otherProjectile.sphereFrictionStrength);

        if (frictionCoefficient <= 0f)
            return;

        Vector3 thisContactOffset =
            collisionNormalDirection * this.sphereRadius;

        Vector3 otherContactOffset =
            -collisionNormalDirection * otherProjectile.sphereRadius;

        Vector3 thisAngularVelocity = this.angularVelocityRad;
        Vector3 otherAngularVelocity = otherProjectile.angularVelocityRad;

        Vector3 thisContactPointVelocity =
            this.linearVelocity + Vector3.Cross(thisAngularVelocity, thisContactOffset);

        Vector3 otherContactPointVelocity =
            otherProjectile.linearVelocity + Vector3.Cross(otherAngularVelocity, otherContactOffset);

        Vector3 relativeVelocityAtContact =
            thisContactPointVelocity - otherContactPointVelocity;

        Vector3 tangentialRelativeVelocity =
            relativeVelocityAtContact
            - Vector3.Dot(relativeVelocityAtContact, collisionNormalDirection)
            * collisionNormalDirection;

        if (tangentialRelativeVelocity.sqrMagnitude < 1e-8f)
            return;

        float combinedEffectiveMass =
            1f / (1f / this.sphereMass + 1f / otherProjectile.sphereMass);

        // Jt = -k * vT * dt * mEff
        Vector3 tangentialImpulse =
            (-frictionCoefficient * deltaTime * combinedEffectiveMass)
            * tangentialRelativeVelocity;

        this.linearVelocity += tangentialImpulse / this.sphereMass;
        otherProjectile.linearVelocity -= tangentialImpulse / otherProjectile.sphereMass;

        this.angularMomentum += Vector3.Cross(thisContactOffset, tangentialImpulse);
        otherProjectile.angularMomentum +=
            Vector3.Cross(otherContactOffset, -tangentialImpulse);
    }


    bool SphereIntersectsAabb(Vector3 sphereCenter, float radius, Vector3 boxMin, Vector3 boxMax)
    {
        // Box nearest point
        float x = Mathf.Clamp(sphereCenter.x, boxMin.x, boxMax.x);
        float y = Mathf.Clamp(sphereCenter.y, boxMin.y, boxMax.y);
        float z = Mathf.Clamp(sphereCenter.z, boxMin.z, boxMax.z);

        Vector3 closestPoint = new Vector3(x, y, z);

        // collision
        return (sphereCenter - closestPoint).sqrMagnitude <= radius * radius;
    }

    void DestroySelf()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        if (GameManager.Instance != null)
            GameManager.Instance.NotifyProjectileFinished(this);

        Destroy(gameObject);
    }
}