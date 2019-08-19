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

//[DisableAutoCreation]
[UpdateAfter (typeof(QuadrantSystem))]

public class CollisionJobSystem : JobComponentSystem
{
    //protected override void OnUpdate()
    //{
    //    Entities.WithAll<Planetoid>().ForEach((Entity _activeEntity, ref Translation activeTranslation, ref MotionProperties _activeMotionProperties) =>
    //    {
    //        float3 position = activeTranslation.Value;
    //        MotionProperties activeMotionProperties = _activeMotionProperties;
    //        float3 direction = activeMotionProperties.direction;
    //        Entity activeEntity = _activeEntity;
    //        //en
    //        Entities.WithAll<Planetoid>().ForEach((Entity passiveEntity, ref Translation passiveTranslation, ref MotionProperties passiveMotionProperties, ref Scale scale) =>
    //        {
    //            if (position.x < passiveTranslation.Value.x)
    //            {
    //                //for each entity calculate direction, then calculate collision distance for both entities
    //                //then compare if they are colliding
    //                //the half of the shortest and logest radius multiplied by scale?
    //                float distance = math.distancesq(position, passiveTranslation.Value);
    //                if (distance < 0.3f) 
    //                {
    //                    Debug.Log("Collided!");
    //                    float3 tempDirection = passiveMotionProperties.direction;
    //                    passiveMotionProperties.direction = activeMotionProperties.direction;
    //                    direction = tempDirection;
    //                    PostUpdateCommands.DestroyEntity(passiveEntity);
    //                    PostUpdateCommands.DestroyEntity(activeEntity);
    //                }
    //            }
    //        });
    //        if (activeEntity != Entity.Null)
    //        {
    //            activeMotionProperties.direction = direction;
    //        }
    //    });
    //}

    #region CollisionJobSystemWithoutBurst
    [RequireComponentTag (typeof(Planetoid))]
    private struct CollisionDetectionJob : IJobForEachWithEntity<Translation>
    {
        [DeallocateOnJobCompletion] [ReadOnly] public NativeArray<EntityWithProps> entitiesWithProps;
        //nativemultihash
        public EntityCommandBuffer.Concurrent entityCommandBuffer;
        public void Execute(Entity entity, int index, ref Translation translation)
        {
            float3 position = translation.Value;
            for (int i = 0;  i < entitiesWithProps.Length; i++)
            {
                EntityWithProps entityWithProps = entitiesWithProps[i];
                if (position.x < entityWithProps.position.x)
                {
                    float distance = math.distancesq(position, entityWithProps.position);
                    if (distance < 0.3f)
                    {
                        //Debug.Log("Asteroids destroid, pending to respawn");
                        entityCommandBuffer.DestroyEntity(index, entity);
                        entityCommandBuffer.DestroyEntity(index, entityWithProps.entity);
                        //instead make an array of all destroyed asteroids and deal with them later
                    }
                }
            }
        }
    }
    #endregion
    #region SortPositionsByPairs
    [BurstCompile]
    public struct SortPositionsJob : IJob
    {
        [NativeDisableContainerSafetyRestriction] [ReadOnly] public NativeMultiHashMap<int, EntityWithProps> quadrantMultiHashMap;
        [ReadOnly] public NativeArray<int> keys;
        [ReadOnly] public float realTimeSinceStartUp;
        [NativeDisableContainerSafetyRestriction] [DeallocateOnJobCompletion] public NativeArray<EntityWithProps> sortedPositionsArray; //mind indexes for each key of end and start
        //[NativeDisableParallelForRestriction] public NativeQueue<Entity>.Concurrent destroyedPlanetoidsQueue;

        public int index;
        public void Execute()
        {
            if (quadrantMultiHashMap.TryGetFirstValue(keys[index], out EntityWithProps inspectedEntityWithProps, out NativeMultiHashMapIterator<int> it))
            {
                do
                {
                    for (int i = 0; i < sortedPositionsArray.Length; i++)
                    {
                        if (i == 0)
                        {
                            sortedPositionsArray[i] = inspectedEntityWithProps;
                        }
                        else if (inspectedEntityWithProps.vecSqrMag > sortedPositionsArray[i - 1].vecSqrMag)
                        {
                            sortedPositionsArray[i] = inspectedEntityWithProps;
                        }
                        else
                        {
                            int j = i;
                            while (j - 1 >= 0 && inspectedEntityWithProps.vecSqrMag < sortedPositionsArray[j - 1].vecSqrMag)
                            {
                                EntityWithProps tempEntityWithProps = sortedPositionsArray[j - 1];
                                sortedPositionsArray[j - 1] = inspectedEntityWithProps;
                                sortedPositionsArray[j] = tempEntityWithProps;
                                j--;
                            }
                        }
                    }
                }
                while (quadrantMultiHashMap.TryGetNextValue(out inspectedEntityWithProps, ref it));//TODO can add a boolean if need to stop after first crash
            }
            for (int i = 0; i < sortedPositionsArray.Length-1; i++)
            {   //adding planetoids with crash distance to queue
                EntityWithProps tempEntityWithProps;
                if (sortedPositionsArray[i + 1].vecSqrMag - sortedPositionsArray[i].vecSqrMag < 0.3f)
                {
                    //tempEntityWithProps = sortedPositionsArray[i];
                    //tempEntityWithProps.crashTime = realTimeSinceStartUp;
                    //sortedPositionsArray[i] = tempEntityWithProps;
                    //tempEntityWithProps = sortedPositionsArray[i + 1];
                    //tempEntityWithProps.crashTime = realTimeSinceStartUp;
                    //sortedPositionsArray[i + 1] = tempEntityWithProps;
                }
            }
            //TODO add everything to cumulative array OR build a static list of crash times and a separate system that would create or revive entities, 
        }
    }
    #endregion

    //TODO reset Planetoid crashTime of all active planetoidsprivate 

    [RequireComponentTag(typeof(RenderMesh))]
    [ExcludeComponent (typeof(Disabled))]
    [BurstCompile]
    private struct CollisionDetectionInQuadrantsJob : IJobForEachWithEntity<Translation, MotionProperties, Planetoid>
    {
        [ReadOnly] public NativeMultiHashMap<int, EntityWithProps> quadrantMultiHashMap;
        public NativeQueue<Entity>.Concurrent destroyedPlanetoidsQueue;
        public NativeQueue<float3>.Concurrent explosionCoordsQueue;
        //public NativeQueue<float>.Concurrent crashTimesQueue;
        //public EntityCommandBuffer.Concurrent entityCommandBuffer;
        public float realTimeSinceStartUp;
        public void Execute(Entity entity, int index, ref Translation translation, [ReadOnly] ref MotionProperties motionProperties, ref Planetoid planetoidCrashInfo)
        {
            float3 position = translation.Value;
            int hashMapKey = QuadrantSystem.GetPositionHashMapKey(position);
               
            if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out EntityWithProps inspectedEntityWithProps, out NativeMultiHashMapIterator<int> it))//TODO check the neighbouring quadrants
            {   //TODO find the closest entity and only monitor that one
                do
                {
                    float distance = math.distancesq(position, inspectedEntityWithProps.position);
                    /*TODO for ellipses:
                     * calculate angle vector to the target
                     * add angle to rotation angle of active ellipse collider
                     * calculate radius of active ellipse 
                     * do the same with passive ellipse collider
                     * sum the radiae to get the distance                    
                     */                   
                    if (distance < 0.3f && inspectedEntityWithProps.entity != entity && !(inspectedEntityWithProps.planetoidCrashData.crashTime > 0))// TODO and if entity that is being collided with has crashTime zero
                    {
                        planetoidCrashInfo.crashTime = realTimeSinceStartUp;
                        inspectedEntityWithProps.planetoidCrashData.crashTime = realTimeSinceStartUp;
                        destroyedPlanetoidsQueue.Enqueue(entity);
                        destroyedPlanetoidsQueue.Enqueue(inspectedEntityWithProps.entity);
                        //explosion location, TODO put exlosion locations into a persistent queue
                        float3 explosionPoint = position - (position - inspectedEntityWithProps.position) / 2;
                        explosionCoordsQueue.Enqueue(explosionPoint);
                        break;
                     
                    }
                    
                }
                while (quadrantMultiHashMap.TryGetNextValue(out inspectedEntityWithProps, ref it));
               
            }
        }
    }
    #region AddRemoveComponents
    private struct RemoveTranslationParallelJob : IJobParallelFor
    {
        public NativeList<Entity> destroyedPlanetoidsQueue;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;
        public float realTimeSinceStartUp;

        public void Execute (int index)
        {
            Entity entity = destroyedPlanetoidsQueue[index];
            entityCommandBuffer.RemoveComponent<Translation>(index, entity);

        }
    }



    private struct AddDisabledComponentJob : IJob // first must be copied to array
    {
        public NativeQueue<Entity> destroyedPlanetoidQueue;
        //public NativeArray<Entity> destroyedPlanetoidArray;
        public EntityCommandBuffer entityCommandBuffer;

        public void Execute()
        {
            while(destroyedPlanetoidQueue.TryDequeue(out Entity entity))
            {
                //entityCommandBuffer.RemoveComponent<RenderMesh>(entity);
               
                entityCommandBuffer.AddComponent(entity, new Disabled { });


            }
        }
    }

    private struct NativeQueueToArrayJob : IJob
    {
        public NativeQueue<Entity> destroyedPlanetoidQueue;
        public NativeArray<Entity> destroyedPlanetoidArray;

        public void Execute()
        {
            int index = 0;
            Entity renderData;
            while (destroyedPlanetoidQueue.TryDequeue(out renderData))
            {
                destroyedPlanetoidArray[index] = renderData;
                index++;
            }
        }
    }

    private struct AddTranslationJob : IJobForEachWithEntity<Disabled, Translation, Planetoid>
    {
        [ReadOnly] public float realTimeSinceStartUp;
        public EntityCommandBuffer.Concurrent entityCommandBuffer;
        [ReadOnly] public float3 cameraMaxLeftDown;
        [ReadOnly] public float3 cameraMaxRightUp;
        [ReadOnly] public float seed;
        public void Execute (Entity entity, int index, [ReadOnly] ref Disabled isDeactivated, ref Translation translation, ref Planetoid planetoidCrashInfo)
        {
            int gridEdgeCells = 300;
            float scale = 5;
            float timePassedSinceCrash = realTimeSinceStartUp - planetoidCrashInfo.crashTime;
           
            if (timePassedSinceCrash > 3)
            {
                var random = new Unity.Mathematics.Random((uint) seed + (uint) index);
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
                //entityCommandBuffer.RemoveComponent<IsDeactivated>(index, entity);
                //entityCommandBuffer.AddSharedComponent(index, entity, new RenderMesh { mesh = GameHandler.meshData.mesh, material = GameHandler.meshData.material });
                translation.Value = nextPosition;
                planetoidCrashInfo.crashTime = 0;
            }
        }
    }
    /*first write all destroyed objects to queue, pass to job real time since startup, write it in the queue, make this queue public static in a separate system that will respawn the entities, when writing to queue, or
    after remove translation component, on respawn create that component anew 
    or destroy entities and put in the queue just crash time and respawn queues(requires entitiyManager, costly)
    add all entities regardless of time, to then have translation taken away (Exclude Translation) and in a separate queue add one time per couple or a stuct with number of entities to respawn   
        */
    #endregion
    private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    private EndPresentationEntityCommandBufferSystem endPresentationSimulationEntityCommandBufferSystem;
    protected override void OnCreate()
    {
        endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        endPresentationSimulationEntityCommandBufferSystem =  World.GetOrCreateSystem<EndPresentationEntityCommandBufferSystem>();
        base.OnCreate();
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        NativeQueue<Entity> destroyedPlanetoidsQueue = new NativeQueue<Entity>(Allocator.TempJob);
        #region alternativeNoTWorking
        /*
        NativeArray<int> keys = QuadrantSystem.quadrantMultiHashMap.GetKeyArray(Allocator.TempJob);
        NativeArray<JobHandle> jobHandles = new NativeArray<JobHandle>(keys.Length, Allocator.TempJob);

        //make array of keys
        //in a job make subarray of key, index of a key is number of array
        //loop through array and add to queue
        //place all the subarrays in array
        //use 

        for (int i = 0; i < keys.Length; i++)
        {
            NativeArray<EntityWithProps> entityWithPropsArray = new NativeArray<EntityWithProps>(QuadrantSystem.GetEntityCountInQuadrant(QuadrantSystem.quadrantMultiHashMap, keys[i]), Allocator.TempJob);
            SortPositionsJob sortPositionsJob = new SortPositionsJob
            {
                quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap,
                sortedPositionsArray = entityWithPropsArray,
                realTimeSinceStartUp = Time.realtimeSinceStartup,
                keys = keys,
                index = i
            };
            
            jobHandles[i] = sortPositionsJob.Schedule();
        }

        JobHandle.CompleteAll(jobHandles);
        keys.Dispose();
        jobHandles.Dispose();
        destroyedPlanetoidsQueue.Dispose();
        */
        #endregion
        CollisionDetectionInQuadrantsJob collisionDetectionInQuadrantsJob = new CollisionDetectionInQuadrantsJob
        {
            quadrantMultiHashMap = QuadrantSystem.quadrantMultiHashMap,
            destroyedPlanetoidsQueue = destroyedPlanetoidsQueue.ToConcurrent(),
            explosionCoordsQueue = AnimationHandler.explosionCoordsQueue.ToConcurrent(),
            //entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer().ToConcurrent(),
            realTimeSinceStartUp = Time.realtimeSinceStartup,
        };
        JobHandle jobHandle = collisionDetectionInQuadrantsJob.Schedule(this, inputDeps);
        //endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle);

        AddDisabledComponentJob removeTranslationJob = new AddDisabledComponentJob
        {
            destroyedPlanetoidQueue = destroyedPlanetoidsQueue,
            entityCommandBuffer = endSimulationEntityCommandBufferSystem.CreateCommandBuffer()
        };

        jobHandle = removeTranslationJob.Schedule(jobHandle);
        endSimulationEntityCommandBufferSystem.AddJobHandleForProducer(jobHandle); //executes only after job has been completed 

        jobHandle.Complete();
        destroyedPlanetoidsQueue.Dispose();
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
