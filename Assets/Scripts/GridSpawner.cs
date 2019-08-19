using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Jobs;
using Unity.Jobs;



public class GridSpawner : MonoBehaviour
{
    public int gridEdgeCells = 50;
    public static int scale = 5;
    public float minSpeed, maxSpeed;
    int asteroidCount;

    Dictionary<int, GameObject> asteroidSet = new Dictionary<int, GameObject>();//TODO change to hashset

    GameObject[] asteroidArray;
    AsteroidInfo[] asteroidInfos;
    float[] asteroidSpeeds;
    Vector3[] asteroidDirections;
    Transform[] asteroidTransforms;
    JobHandle jobHandle;


    Vector2[,] spawningCoords;

    public GameObject asteroid;
    public GameObject player;
    public GameObject asteroids;

    private void Awake()
    {
        SpawnPlayer();
        //Time.timeScale = 0;
        asteroidCount = gridEdgeCells * gridEdgeCells;
        asteroidArray = new GameObject[asteroidCount];
        asteroidInfos = new AsteroidInfo[asteroidCount];
        asteroidSpeeds = new float[asteroidCount];
        asteroidDirections = new Vector3[asteroidCount];
        asteroidTransforms = new Transform[asteroidCount];
    }

    void Start()
    {
        Random.seed = 101;
        GetSpawnCoords();
        SpawnAsteroids();
        //CalculatePosition();
    }

    private void GetSpawnCoords()
    {
        spawningCoords = new Vector2[gridEdgeCells, gridEdgeCells];

        for (int i = 0; i < gridEdgeCells; i++)
        {
            for (int j = 0; j < gridEdgeCells; j++)
            {
                spawningCoords[i, j] = new Vector2((i + 0.5f) - (gridEdgeCells / 2), (j + 0.5f) - gridEdgeCells / 2)*scale;
            }
        }
    }

    private void SpawnAsteroids()
    {
        for (int i = 0; i < gridEdgeCells; i++)
        {
            for (int j = 0; j < gridEdgeCells; j++)
            {
                GameObject asteroidInstance = Instantiate(asteroid, spawningCoords[i, j], Quaternion.identity, asteroids.transform);
                
                int _id = asteroidInstance.GetInstanceID();
                asteroidSet.Add(_id, asteroidInstance);
                asteroidInstance.GetComponent<Asteroid>().asteroidDestroyed += OnAsteroidDestroyed;

                asteroidArray[i * gridEdgeCells + j] = asteroidInstance;
                asteroidSpeeds[i * gridEdgeCells + j] = SetSpeed();
                asteroidDirections[i * gridEdgeCells + j] = SetDirection();
                //asteroidInfos[i * gridEdgeCells + j] = new AsteroidInfo((i * gridEdgeCells + j), _id, SetSpeed(), SetDirection());
                asteroidTransforms[i * gridEdgeCells + j] = asteroidInstance.transform;
            }
        }
    }

    public struct AsteroidInfo
    {
        //public bool isVisible;
        public int index;
        public int id;
        public float speed;
        Vector3 direction;

        public AsteroidInfo(int _index,int _id, float _speed, Vector3 _direction)
        {
            index = _index;
            id = _id;
            speed = _speed;
            direction = _direction;

        }

    }

    void SpawnPlayer()
    {
        player = Instantiate(player, transform.position, Quaternion.identity);
    }

    public void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(transform.position, new Vector3(gridEdgeCells, gridEdgeCells)*scale);
    }

    void OnAsteroidDestroyed(int id)
    {
        GameObject asterInstance = asteroidSet[id];

        Vector3 nextPosition = new Vector3(Random.Range(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), Random.Range(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), 0) * scale + transform.position;
        Vector3 cameraMaxLeftDown = Camera.main.ViewportToWorldPoint(new Vector3(0, 0));
        Vector3 cameraMaxRightUp = Camera.main.ViewportToWorldPoint(new Vector3(1, 1));

        if (nextPosition.x > cameraMaxLeftDown.x && nextPosition.x < cameraMaxRightUp.x && nextPosition.y > cameraMaxLeftDown.y && nextPosition.y < cameraMaxRightUp.y)
        {
            if (nextPosition.x < 0)
            {
                nextPosition.x -= cameraMaxRightUp.x;
            }
            else { nextPosition.x += cameraMaxRightUp.x; }
            if (nextPosition.y < 0)
            {
                nextPosition.y -= cameraMaxRightUp.y;
            }
            else { nextPosition.y += cameraMaxRightUp.y; }
        }

        asterInstance.transform.position = nextPosition;
        asterInstance.SetActive(true);
        asterInstance.GetComponent<SpriteRenderer>().enabled = false;
    }

    //private void OnTriggerEnter2D(Collider2D collision)
    //{
    //    if (collision.gameObject.tag == "Asteroid")
    //    {
    //        SpriteRenderer sp = collision.gameObject.GetComponent<SpriteRenderer>();
    //        sp.enabled = !sp.enabled;

    //    }
    //}
    //private void OnTriggerExit2D(Collider2D collision)
    //
    //    if (collision.gameObject.tag == "Asteroid")
    //    {
    //        SpriteRenderer sp = collision.gameObject.GetComponent<SpriteRenderer>();
    //        sp.enabled = !sp.enabled;

    //    }
    //}
    private void Update()
    {
        jobHandle.Complete();

        TransformAccessArray transformAccessArray = new TransformAccessArray(asteroidTransforms);
        NativeArray<float> nativeAsteroidSpeeds = new NativeArray<float>(asteroidSpeeds, Allocator.Persistent);
        NativeArray<Vector3> nativeAsteroidDirections = new NativeArray<Vector3>(asteroidDirections, Allocator.Persistent);

        MoveJob moveJob = new MoveJob();
        moveJob.speeds = nativeAsteroidSpeeds;
        moveJob.directions = nativeAsteroidDirections;

        jobHandle = moveJob.Schedule(transformAccessArray);

        transformAccessArray.Dispose();
        nativeAsteroidSpeeds.Dispose();
        nativeAsteroidDirections.Dispose();
    }
    private void CalculatePosition()
    {
        while (true)
        {
            Parallel.ForEach(asteroidSet, (item) =>
            {
                float speed = item.Value.GetComponent<Asteroid>().speedMultiplier;
                Vector3 direction = item.Value.GetComponent<Asteroid>().initialDirection;
                item.Value.GetComponent<Asteroid>().positions.Enqueue(item.Value.transform.position + speed * direction);
            });
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

    //TODO set thread priority for asteroids that are within the field of view
}
