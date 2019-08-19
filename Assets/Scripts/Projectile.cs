using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    public float deathTime = 3f;
    private float lifeTime;
    private float birthTime;
    private Vector3 direction;


    private void Start()
    {
        birthTime = Time.timeSinceLevelLoad;
        direction = transform.rotation.eulerAngles;
    }

    private void Update()
    {
        lifeTime = Time.timeSinceLevelLoad - birthTime;
        if(lifeTime > deathTime)
        {
            Destroy(gameObject);
        }

        transform.position += transform.up * speed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        SendMessageUpwards("TargetHit", SendMessageOptions.DontRequireReceiver);
        Destroy(other.gameObject);
        Destroy(gameObject);
    }
}
