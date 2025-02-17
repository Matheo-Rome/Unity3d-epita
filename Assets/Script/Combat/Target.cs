using MimicSpace;
using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 30f;
    public float corruption = 50f;
    [SerializeField] private Renderer ren;
    [SerializeField] private Transform portal;
    [SerializeField] private Transform kaboom;
    GameObject bunny;
    GlobalAchievement ach;
    private bool spawnedB = false;

    void Start()
    {
        ren.material.color = Color.black;
        ach = FindObjectOfType<GlobalAchievement>();
        bunny = GameObject.FindGameObjectWithTag("Bunny");
    }

    //Change the already instantiated legs color of the enemy 
    private void changeLegColor(Color color)
    {
         var legs = gameObject.GetComponentsInChildren<Leg>();
         foreach (var leg in legs)
         {
             leg.gameObject.GetComponent<LineRenderer>().material.color = color;
         }
    }
        

    //Remove health and change entity color
    public void TakeDamage(float amount)
    {
        health -= amount;
        switch (health)
        {
            case <= 0f:
                Instantiate(kaboom, transform.position, Quaternion.identity);
                ach.killed++;
                Die();
                break;
            case <= 10f:
                ren.material.color = Color.magenta;
                changeLegColor(Color.magenta);
                break;
            case <= 20f:
                ren.material.color = Color.red;
                changeLegColor(Color.red);
                break;    
            default:
                ren.material.color = Color.black;
                changeLegColor(Color.black);
                break;
        }
    }

    //Remove corruption and change entity color
    public void TakeHealing(float amount)
    {
        corruption -= amount;
        switch (corruption)
        {
            case <= 0f:
                if (!spawnedB)
                {
                    spawnedB = true;
                    Transform createdPortal = Instantiate(portal, transform.position + Vector3.up, Quaternion.identity);
                    createdPortal.forward = transform.forward;
                    Instantiate(bunny, createdPortal.position, transform.rotation);
                    Destroy(createdPortal.gameObject, 1f);
                    ach.saved++;
                    Die();
                }

                break;
                case <= 10f:
                ren.material.color = Color.cyan;
                changeLegColor(Color.cyan);
                break;
            case <= 20f:
                ren.material.color = Color.blue;
                changeLegColor(Color.blue);
                break;
            default:
                ren.material.color = Color.black;
                changeLegColor(Color.black);
                break;
        }
    }

    //Die
    void Die()
    {
        Destroy(gameObject);
    }

}
