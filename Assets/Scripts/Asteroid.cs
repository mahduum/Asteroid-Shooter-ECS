using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;


public class Asteroid : MonoBehaviour
{
    public float minSpeed, maxSpeed;
    int id;

    Collider2D collider;
    Rigidbody2D rigidbody;
    public Vector3 initialDirection;
    SpriteRenderer spriteRenderer;
    public Queue<Vector3> positions = new Queue<Vector3>();
    Vector3 position;

    public float speedMultiplier;
    bool readyToRespawn;

    public delegate void AsteroidDestroyedHandler(int id);
    public event AsteroidDestroyedHandler asteroidDestroyed;

    void Start()
    {
        //initialDirection = SetDirection();
        //speedMultiplier = SetSpeed();
        //position = transform.position;
        //spriteRenderer = GetComponent<SpriteRenderer>();
        rigidbody = GetComponent<Rigidbody2D>();
        collider = GetComponent<Collider2D>();
        //rigidbody.AddForce(initialDirection * speedMultiplier);
        //SetVisibility();
        //Thread thread = new Thread(NextPosition);
        //thread.Start();
    }


    private void SetVisibility()
    {
        Vector3 cameraMaxLeftDown = Camera.main.ViewportToWorldPoint(new Vector3(0, 0));
        Vector3 cameraMaxRightUp = Camera.main.ViewportToWorldPoint(new Vector3(1, 1));

        if (transform.position.x > cameraMaxLeftDown.x && transform.position.x < cameraMaxRightUp.x && transform.position.y > cameraMaxLeftDown.y && transform.position.y < cameraMaxRightUp.y)
        {
            spriteRenderer.enabled = true;
        }
        else
        {
            spriteRenderer.enabled = false;
        }
    }

    void Update()
    {
        //transform.position += initialDirection * speedMultiplier;
        //transform.position = NextPosition();

        //    if (positions.Count > 0)
        //    {
        //        transform.position = positions.Dequeue();
        //    }
        //}
    }
        void NextPosition()
        {
            //var nextPosition = position;
            while (positions.Count < 1000)
            {
                var nextPosition = position + speedMultiplier * initialDirection;
                position = nextPosition;
                positions.Enqueue(position);
                Debug.Log(nextPosition);
            }
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.otherCollider.tag == "Asteroid")
            {
                gameObject.SetActive(false);
                //Invoke("FalseDestroy", 1f);
                //FalseDestroy();
            }
            //if (collision.otherCollider.tag == "Grid")
            //{
            //    Debug.Log("hit the grid");
            //    spriteRenderer.enabled = !enabled;
            //}

        }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log(collision);
    }

    void FalseDestroy()
        {
            OnAsteroidDestroyed(gameObject.GetInstanceID());
        }

    protected virtual void OnAsteroidDestroyed(int id)
    {
        if (asteroidDestroyed != null)
        {
            asteroidDestroyed(id);
        }
    }

    Vector3 SetDirection()
    {
        float x = Random.Range(-1f, 1f);
        float z = Random.Range(-1f, 1f);

        return new Vector3(x, z, 0f);
    }

    float SetSpeed()
    {
        return Random.Range(minSpeed, maxSpeed);
    }
}
