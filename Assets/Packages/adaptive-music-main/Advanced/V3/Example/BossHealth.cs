using UnityEngine;

/// <summary>
/// Stub class for the BossFightManager example.
/// Replace this with your actual boss health implementation.
/// </summary>
public class BossHealth : MonoBehaviour
{
    public float currentHealth = 100f;
    public float maxHealth = 100f;

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        if (currentHealth < 0)
            currentHealth = 0;
    }
}
