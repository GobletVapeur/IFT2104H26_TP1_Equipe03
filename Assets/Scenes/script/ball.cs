
using UnityEngine;


public class ball : MonoBehaviour
{
    public float gravity = 0f;
    private float velocity = 0f;
    private Vector3 direction = Vector3.zero;
    private int masses = 10;
    private int force = 0;
    private Vector3 spawn;
    GameObject wall;
    float wallBottomY = 0f;
  

    //la sphere rayon
    float radius;
    //f=m*a
    int simple_timer = 0;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        var rend = GetComponent<Renderer>();
       
            Vector3 extents = rend.bounds.extents;
            radius = extents.magnitude;
        
       


        
            wall = GameObject.FindGameObjectWithTag("wall");    

            if (wall != null)
            {
                var rendWall = wall.GetComponent<Renderer>();
                if (rendWall != null)
                {
                    var hauteurwallbas = rendWall.bounds.extents;
                    wallBottomY = wall.transform.position.y - hauteurwallbas.y;

                }
              
            }




        endturn();

        
    } 
    
    void Update()
    {

        if (simple_timer == 200)
        {
            GameObject.FindGameObjectWithTag("Respawn").transform.position=spawn;
            transform.position= spawn;
            this.Start();
            simple_timer = 0;
        }
        if (intersection(transform.position))
        {
         
            simple_timer++;

            // 
            Vector3 movement = direction * velocity;
            Vector3 normal = Vector3.up;
            Vector3 reflected = Vector3.Reflect(movement, normal);

            // placer la balle au dessu du mur/plancher
            Vector3 pos = transform.position;
            pos.y = wallBottomY + radius + 0.001f;//merci copilot
            transform.position = pos;

            
            float forceFactor = 0.8f; 
            velocity = reflected.magnitude * forceFactor;

            if (velocity <= 0.001f)
            {
                
                velocity = 0f;
                direction = Vector3.zero;
                gravity = 0f;
            }
            else
            {
                direction = reflected.normalized;
                gravity = Mathf.Abs(gravity);
            }
        }
        else
        {
            velocity -= gravity * Time.deltaTime;
            transform.Translate(direction * velocity * Time.deltaTime);
        }
        

    }
    //meme si la balle passe le sol on sait qu'il y a eu une collision
    private bool intersection(Vector3 position) {
        //to do si nessesaire ajouter les test pour les autre face/ avec force ver autres direction
       if (wall == null) return false;

       float spherebas = position.y - radius;
       if (spherebas <= wallBottomY)
       {
         return true;
       }
       return false;
    }
    private void endturn()
    {
        velocity = Random.Range(5f, 10f);
        direction = Vector3.up;
        spawn = transform.position;
        force = (int)velocity * masses;
        gravity = 9.8f;

    }
}
