using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Cannons")]
    public Cannon cannonPlayer1;
    public Cannon cannonPlayer2;

    [Header("Turn properties")]
    public int projectilesPerTurn = 10;
    public float timeBetweenShots = 0.10f; 
    public float timeBetweenTurns = 0.50f;
    public float projectileLifetimeSeconds = 7f;

    private bool isPlayer1Turn = true;
    
    private readonly HashSet<Projectile> livingProjectiles = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // Access to WaitForSeconds
        StartCoroutine(TurnLoop());
    }

    IEnumerator TurnLoop()
    {
        while (true)
        {
            Cannon activeCannon = isPlayer1Turn ? cannonPlayer1 : cannonPlayer2;

            livingProjectiles.Clear();
            
            for (int projectileIteration = 0; projectileIteration < projectilesPerTurn; projectileIteration++)
            {
                
                Projectile projectile = activeCannon.CreateProjectile();
                if (projectile != null)
                {
                    projectile.maxLifetimeSeconds = projectileLifetimeSeconds;
                    livingProjectiles.Add(projectile);
                }

                yield return new WaitForSeconds(timeBetweenShots);
            }

            // Allows turn based simulation
            while (livingProjectiles.Count > 0)
                yield return null;

            yield return new WaitForSeconds(timeBetweenTurns);

            isPlayer1Turn = !isPlayer1Turn;
        }
    }
    
    public void NotifyProjectileFinished(Projectile projectile)
    {
        if (projectile != null)
            livingProjectiles.Remove(projectile);
    }

    // For sphere to sphere collisions
    public Projectile[] GetProjectilesSnapshot()
    {
        return new List<Projectile>(livingProjectiles).ToArray();
    }
}