using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Projectiles : MonoBehaviour
{

    public event Action onHitTarget;

    void TargetHit()
    {
        if (onHitTarget != null)
        {
            onHitTarget();
        }
    }
}
