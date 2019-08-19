using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Player : MonoBehaviour
{

    public float rotatingSpeed;
    public float movingSpeed;
    public float firingRate = 0.5f;
 
    public GameObject projectile;
    public Vector3 projectileOffset = new Vector3(0, 0.5f, 0);

    public float firingTimer = 0;

    public event Action playerDeathEvent;

    private Projectiles projectiles;

    void Start()
    {
        projectiles = FindObjectOfType<Projectiles>();
    }

    void Update()
    {
        if (firingTimer == 0)
        {
            //Instantiate(projectile, transform.position + transform.up/2, transform.rotation, projectiles.transform);
        }
        if (firingTimer < firingRate)
        {
            firingTimer += Time.deltaTime;         
        }
        else
        {
            firingTimer = 0;
        }
        MovePlayer();
    }

    void MovePlayer()
    {
        transform.Rotate(0, 0, -Input.GetAxisRaw("Horizontal") * rotatingSpeed);
        transform.Translate(0, Input.GetAxisRaw("Vertical") * movingSpeed, 0, Space.Self);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (playerDeathEvent != null)
        {
            playerDeathEvent(); 
        }
        Destroy(gameObject);
        Time.timeScale = 0;
    }
}
