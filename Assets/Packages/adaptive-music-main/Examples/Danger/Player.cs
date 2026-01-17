using UnityEngine;

public class Player : MonoBehaviour
{
    private int health = 100;
    private bool dead=false;
    
    public void PlayerDamage(int damage)
    {
        health -= damage;

        // Trigger the Danger method (static)
        GameMusic.Danger();

        if (health <= 0)
        {
            if(!dead)
            {
                Die();
            }
        }
    }

    private void Die()
    {
        // Trigger the Death method (static)
        GameMusic.Death();

        // Perform any other necessary actions upon player death
    }
}
