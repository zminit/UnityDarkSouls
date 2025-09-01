using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerState : MonoBehaviour
{
    [SerializeField]
    private float maxHP = 100f;
    [SerializeField]
    private float HP = 100f;
    [SerializeField]
    private float maxMP = 50f;
    [SerializeField]
    private float MP = 50f;

    public bool IsInvincible = false;
    public bool IsDead = false;

    public void GetDamage(float damage)
    {
        if(IsInvincible) return;
        
        HP -= damage;
        if (HP < 0) HP = 0;
        
        // Optionally, you can add logic to handle player death here
        if (HP == 0)
        {
            Debug.Log("Player has died.");
            // Handle player death logic
        }
    }

    public void StartInvincible()
    {
        IsInvincible = true;
    }

    public void EndInvincible()
    {
        IsInvincible = false;
    }

}
