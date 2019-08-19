using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{//
    GameObject explosion;
   
    private void Extinguish()
    {
        Destroy(gameObject);
        print("Extinguished");
    }

}
