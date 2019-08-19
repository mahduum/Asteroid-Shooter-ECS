using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using Unity.Collections.LowLevel.Unsafe;
using Unity.Rendering;

public struct CrashInfo
{
    public Entity entity;
    public float realtimeSinceStartUp;
}

[DisableAutoCreation]
[UpdateAfter(typeof(QuadrantSystem))]
public class NewCollisionJobSystem : JobComponentSystem
{
   
    //[BurstCompile]
    public struct SortPositionsJob : IJob
    {
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeMultiHashMap<int, EntityWithProps> quadrantMultiHashMap;
        [ReadOnly] public NativeArray<int> keys;
        [ReadOnly] public float realtimeSinceStartUp;
        [NativeDisableContainerSafetyRestriction] [DeallocateOnJobCompletion] public NativeArray<EntityWithProps> entityWithPropsArray; //mind indexes for each key of end and start
        [NativeDisableContainerSafetyRestriction] [DeallocateOnJobCompletion] public NativeArray<Planetoid> planetoids;
        //[NativeDisableParallelForRestriction] public NativeQueue<CrashInfo> destroyedPlanetoidsQueue;
       
        public int index;
        public void Execute()
        {
            if (quadrantMultiHashMap.TryGetFirstValue(keys[index], out EntityWithProps inspectedEntityWithProps, out NativeMultiHashMapIterator<int> it))
            {
                int i = 0;
                do
                {
                    entityWithPropsArray[i] = inspectedEntityWithProps;
                    i++;
                }
                while (quadrantMultiHashMap.TryGetNextValue(out inspectedEntityWithProps, ref it));//TODO can add a boolean if need to stop after first crash
            }
            for (int i = 0; i < entityWithPropsArray.Length - 1; i++)
            {   
                for (int j = i + 1; j < entityWithPropsArray.Length; j++)
                {
                    if (math.distancesq(entityWithPropsArray[i].position, entityWithPropsArray[j].position) < 0.3f)
                    {
                        Planetoid temp = planetoids[entityWithPropsArray[i].entity.Index];
                        temp.crashTime = realtimeSinceStartUp;
                        planetoids[entityWithPropsArray[i].entity.Index] = temp;

                        temp = planetoids[entityWithPropsArray[j].entity.Index];
                        temp.crashTime = realtimeSinceStartUp;
                        planetoids[entityWithPropsArray[j].entity.Index] = temp;


                        //and now GetEntitiyQueryPlanetoids

                        //destroyedPlanetoidsQueue.Enqueue(new CrashInfo { entity = entityWithPropsArray[i].entity, realtimeSinceStartUp = realtimeSinceStartUp });
                        //destroyedPlanetoidsQueue.Enqueue(new CrashInfo { entity = entityWithPropsArray[j].entity, realtimeSinceStartUp = realtimeSinceStartUp });
                        break; //TODO figure out how to keep track on adding multiple asteroids in a single crash
                    }
                }

            }
            //TODO add everything to cumulative array OR build a static list of crash times and a separate system that would create or revive entities, 
        }
    }

    public struct DisableJob : IJobForEachWithEntity<Planetoid>
    {
        public EntityCommandBuffer.Concurrent entityCommandBuffer;
        public void Execute (Entity entity, int index, [ReadOnly] ref Planetoid planetoid)
        {
            if (planetoid.crashTime > 0)
            {
                entityCommandBuffer.AddComponent(index, entity, new Disabled { });
            }
        }
    }
    //TODO put a job here that will sort disabled entities according to the lowest value and add to queue (can be global)
    //TODO or make IJobForEach...

   

    #region AddComponentsJob
   
    private struct AddTranslationJob : IJobForEachWithEntity<Disabled, Translation, Planetoid>
    {
        [ReadOnly] public float realTimeSinceStartUp;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;
        [ReadOnly] public float3 cameraMaxLeftDown;
        [ReadOnly] public float3 cameraMaxRightUp;
        [ReadOnly] public float seed;
        public void Execute(Entity entity, int index, [ReadOnly] ref Disabled disabled, ref Translation translation, ref Planetoid planetoidCrashInfo)
        {
            int gridEdgeCells = 300;
            float scale = 5;
            float timePassedSinceCrash = realTimeSinceStartUp - planetoidCrashInfo.crashTime;

            if (timePassedSinceCrash > 3)
            {
                var random = new Unity.Mathematics.Random((uint)seed + (uint)index);
                float3 nextPosition = new Vector3(random.NextFloat(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), random.NextFloat(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), 0) * scale;
                //float3 nextPosition = new Vector3(UnityEngine.Random.Range(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), UnityEngine.Random.Range(-(float)gridEdgeCells / 2, (float)gridEdgeCells / 2), 0) * scale;

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
                entityCommandBuffer.RemoveComponent<Disabled>(index, entity);//TODO can also be added to list of objects to have components added and coords can be passed to the array of coords
                translation.Value = nextPosition;
                planetoidCrashInfo.crashTime = 0;
            }
        }
    }
    #endregion
    /*first write all destroyed objects to queue, pass to job real time since startup, write it in the queue, make this queue public static in a separate system that will respawn the entities, when writing to queue, or
    after remove translation component, on respawn create that component anew 
    or destroy entities and put in the queue just crash time and respawn queues(requires entitiyManager, costly)
    add all entities regardless of time, to then have translation taken away (Exclude Translation) and in a separate queue add one time per couple or a stuct with number of entities to respawn   
        */

    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EndPresentationEntityCommandBufferSystem endPresentationSimulationEntityCommandBufferSystem;
    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        endPresentationSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndPresentationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        //NativeQueue<CrashInfo> destroyedPlanetoidsQueue = new NativeQueue<CrashInfo>(Allocator.TempJob);
        EntityQuery entityQuery = GetEntityQuery(typeof(Planetoid));
        NativeArray<Planetoid> planetoids = entityQuery.ToComponentDataArray<Planetoid>(Allocator.TempJob);
        NativeArray<int> keys = QuadrantSystem.quadrantMultiHashMap.GetKeyArray(Allocator.TempJob);
        NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(keys.Length, Allocator.TempJob);
        

        for (int i = 0; i < keys.Length; i++)
        {
            NativeArray<EntityWithProps> entityWithPropsArray = new NativeArray<EntityWithProps>(QuadrantSystem.GetEntityCountInQuadrant(QuadrantSystem.quadrantMultiHashMap, keys[i]), Allocator.TempJob);
            SortPositionsJob sortPositionsJob = new SortPositionsJob
            {
                quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap,
                entityWithPropsArray = entityWithPropsArray,
                realtimeSinceStartUp = Time.realtimeSinceStartup,
                planetoids = planetoids,
                keys = keys,
                index = i
            };

            jobHandles[i] = sortPositionsJob.Schedule();

        }

        JobHandle job = JobHandle.CombineDependencies(jobHandles);
        JobHandle.CompleteAll(jobHandles);
        jobHandles.Dispose();
        keys.Dispose();


        //destroyedPlanetoidsQueue.Dispose();

        DisableJob disableJob = new DisableJob
        {
            entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent()
        };

        JobHandle jobHandle = disableJob.Schedule(this, job);//TODO try job handles combine
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

       
        AddTranslationJob addTranslationJob = new AddTranslationJob
        {
            seed = UnityEngine.Random.Range(1, 1000000),
            realTimeSinceStartUp = Time.realtimeSinceStartup,
            entityCommandBuffer = endPresentationSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            cameraMaxLeftDown = Camera.main.ViewportToWorldPoint(new Vector3(0, 0)),
            cameraMaxRightUp = Camera.main.ViewportToWorldPoint(new Vector3(1, 1))
        };
        jobHandle = addTranslationJob.Schedule(this, jobHandle);
        endPresentationSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);
       

    
        return jobHandle;
    }



}