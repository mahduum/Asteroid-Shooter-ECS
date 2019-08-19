using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using System;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

public class GameHandler : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private Material explosionMaterial;

    private Mesh mesh;
    private Mesh explosionMesh;
    public struct MeshData
    {
        public Mesh mesh;
        public Mesh explosionMesh;
        public Material material;
        public Material explosionMaterial;
    }
    public static MeshData meshData;
    private EntityManager entityManager;
    // Start is called before the first frame update
  
    void Start()
    {
        mesh = CreateMesh(0.5f);
        explosionMesh = CreateMesh(1f);
        meshData = new MeshData { mesh = mesh, explosionMesh = explosionMesh, material = material, explosionMaterial = explosionMaterial };
        entityManager = World.Active.EntityManager;
        NativeArray<Entity> entityArray = new NativeArray<Entity>(2500, Allocator.Temp);//Temp is for creating entities
        EntityArchetype entityArchetype = entityManager.CreateArchetype
            (
                typeof(RenderMesh),
                typeof(LocalToWorld),
                typeof(Translation),
                typeof(Rotation),
                //typeof(NonUniformScale),
                typeof(Scale),
                typeof(MotionProperties),
                typeof(Planetoid)
            );
        entityManager.CreateEntity(entityArchetype, entityArray);

        foreach (var entity in entityArray)
        {
            entityManager.SetSharedComponentData(entity, new RenderMesh
            {
                mesh = mesh,
                material = material
            });
            //entityManager.SetComponentData(entity, new NonUniformScale
            //{
            //    Value = new float3(UnityEngine.Random.Range(1f, 5f), UnityEngine.Random.Range(1f, 5f), 0)
            //});
            entityManager.SetComponentData(entity, new Scale() { Value = 1f });
            entityManager.SetComponentData(entity, new Translation
            {
                Value = GetSpawnCoordsReversed(50, 5, entity.Index)//second num is scale TODO make a variable of it
                //Value = new float3(UnityEngine.Random.Range(-8f, 8f), UnityEngine.Random.Range(-5f, 5f), 0)
            });
            entityManager.SetComponentData(entity, new MotionProperties
            {
                moveSpeed = SetSpeed(1f, 2f),
                direction = SetDirection(),
                rotationSpeed = UnityEngine.Random.Range(0f, 1f)
            });

        }
        entityArray.Dispose();
    }

    private Mesh CreateMesh(float halfUnit)//for custom scaled mesh pass width and height as params, then divide by 2 and pass as units in vertices
    {
        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];
        int[] triangles = new int[6];

        vertices[0] = new Vector3(-halfUnit, -halfUnit);
        vertices[1] = new Vector3(-halfUnit, +halfUnit);
        vertices[2] = new Vector3(+halfUnit, +halfUnit);
        vertices[3] = new Vector3(+halfUnit, -halfUnit);

        uv[0] = new Vector2(0, 0);
        uv[1] = new Vector2(0, 1);
        uv[2] = new Vector2(1, 1);
        uv[3] = new Vector2(1, 0);

        triangles[0] = 0;
        triangles[1] = 1;
        triangles[2] = 3;

        triangles[3] = 1;
        triangles[4] = 2;
        triangles[5] = 3;

        Mesh mesh = new Mesh();
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;

        return mesh;
    }

    private void GetSpawnCoords(int gridEdgeCells, int scale)
    {
        Vector3[,] spawningCoords = new Vector3[gridEdgeCells, gridEdgeCells];

        for (int i = 0; i < gridEdgeCells; i++)
        {
            for (int j = 0; j < gridEdgeCells; j++)
            {
                spawningCoords[i, j] = new Vector3((i + 0.5f) - (gridEdgeCells / 2), (j + 0.5f) - gridEdgeCells / 2) * scale;
            }
        }
    }

    private void GetSpawnCoordsArray(int gridEdgeCells, int scale)
    {
        Vector3[] spawningCoords = new Vector3[gridEdgeCells * gridEdgeCells];
        for (int i = 0; i < gridEdgeCells; i++)
        {
            for (int j = 0; j < gridEdgeCells; j++)
            {
                spawningCoords[i * gridEdgeCells + j] = new Vector3((i + 0.5f) - (gridEdgeCells / 2), (j + 0.5f) - gridEdgeCells / 2) * scale;
            }
        }
    }

    private float3 GetSpawnCoordsReversed(int gridEdgeCells, int scale, int index) //reversed because it traces spawn grid backwards by index in one dimentional array
    {//reversed get coordinates based solely on the entity's index, it extracts x and y
        int x = index % gridEdgeCells;
        int y = (index - x) / gridEdgeCells;//or simply foor of the division
      
        return new float3((y + 0.5f) - (gridEdgeCells / 2), (x + 0.5f) - gridEdgeCells / 2, 0) * scale;
    }


    public static float3 SetDirection()
        {
            float x = UnityEngine.Random.Range(-1f, 1f);
            float z = UnityEngine.Random.Range(-1f, 1f);

            return new float3(x, z, 0f);
        }

    public static float SetSpeed(float minSpeed, float maxSpeed)
    {
        return UnityEngine.Random.Range(minSpeed, maxSpeed);
    }

    public static float3 GetRespawnCoordinates(int gridEdgeCells, float scale)
    {
        var random = new Unity.Mathematics.Random(1);
        float3 nextPosition = new Vector3(random.NextFloat(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), random.NextFloat(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), 0) * scale;
        //float3 nextPosition = new Vector3(UnityEngine.Random.Range(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), UnityEngine.Random.Range(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), 0) * scale;
        float3 cameraMaxLeftDown = Camera.main.ViewportToWorldPoint(new Vector3(0, 0));
        float3 cameraMaxRightUp = Camera.main.ViewportToWorldPoint(new Vector3(1, 1));
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
        return nextPosition;
    }


}

public struct Planetoid : IComponentData
{
    public float crashTime;
}

public struct MotionProperties : IComponentData
{
    public float moveSpeed;
    public float3 direction;
    public float rotationSpeed;
}

//[DisableAutoCreation]
public class MoveSystemJob : JobComponentSystem
{

    [BurstCompile]
    private struct MoveJob : IJobForEach<Translation, MotionProperties>
    {
        public float deltaTime;
        //TODO go through native array of MotionProperties that is persistent, or get random seeded values on the fly based on index
        public void Execute(ref Translation translation, [ReadOnly] ref MotionProperties motionProperties)
        {
            translation.Value += motionProperties.direction * motionProperties.moveSpeed * deltaTime;
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        /*
        Entities.ForEach((ref Translation translation, ref MotionProperties motionProperties) =>
            {
                translation.Value += motionProperties.direction * motionProperties.moveSpeed * Time.deltaTime;
            });
            */

        //EntityQuery entityQuery = GetEntityQuery(typeof(Translation), typeof(MotionProperties));
        MoveJob moveJob = new MoveJob
        {
            deltaTime = Time.deltaTime
        };
        return moveJob.Schedule(this, inputDeps);

    }
}
//[DisableAutoCreation]
public class RotatorSystem : JobComponentSystem
{
    [BurstCompile]
    private struct RotationJob : IJobForEach<Rotation, MotionProperties>
    {
        public float realtimeSinceStartUp;
        public void Execute(ref Rotation rotation, ref MotionProperties motionProperties)
        {
            rotation.Value = quaternion.Euler(0, 0, math.PI * realtimeSinceStartUp * motionProperties.rotationSpeed);

        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        /*
        Entities.ForEach((ref Rotation rotation, ref MotionProperties motionProperties) =>
        {
            rotation.Value = quaternion.Euler(0, 0, math.PI * Time.realtimeSinceStartup * motionProperties.rotationSpeed);
        });
        */
        RotationJob rotationJob = new RotationJob
        {
            realtimeSinceStartUp = Time.realtimeSinceStartup
        };
        return rotationJob.Schedule(this, inputDeps);
    }
}

