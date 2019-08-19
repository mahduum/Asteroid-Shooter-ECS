using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using Unity.Jobs;
using CodeMonkey.Utils;
using Unity.Rendering;

public struct EntityWithProps
{
    public Entity entity;
    public float3 position;
    public float vecSqrMag;
    public float crashTime;
    public Planetoid planetoidCrashData;
}
//[DisableAutoCreation]

public class QuadrantSystem : ComponentSystem
{
    private const int quadrantMultiplier = 1000;
    private const int quadrantSize = 20;

    public static int GetPositionHashMapKey(float3 position)
    {
        return (int)(math.floor(position.x / quadrantSize) + (quadrantMultiplier * math.floor(position.y / quadrantSize)));
    }

    private static void DebugDrawQuadrant (float3 position)
    {
        Vector3 lowerLeft = new Vector3(math.floor(position.x / quadrantSize) * quadrantSize, math.floor(position.y / quadrantSize) * quadrantSize, 0);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+0, +1) * quadrantSize);
        Debug.DrawLine(lowerLeft, lowerLeft + new Vector3(+1, +0) * quadrantSize);
        Debug.DrawLine(lowerLeft + new Vector3(+0, +1) * quadrantSize, lowerLeft + new Vector3(+1, +1) * quadrantSize);
        Debug.DrawLine(lowerLeft + new Vector3(+1, +0) * quadrantSize, lowerLeft + new Vector3(+1, +1) * quadrantSize);
    }

    public static int GetEntityCountInQuadrant(NativeMultiHashMap<int, EntityWithProps> quadrantMultiHashMap, int hashMapKey)
    {
        EntityWithProps entity;
        NativeMultiHashMapIterator<int> nativeMultiHashMapIterator;
        int count = 0;
        if (quadrantMultiHashMap.TryGetFirstValue(hashMapKey, out entity, out nativeMultiHashMapIterator)){
            do
            {
                count++;
            } while (quadrantMultiHashMap.TryGetNextValue(out entity, ref nativeMultiHashMapIterator));
        }
        return count;
    }

   
    //[RequireComponentTag (typeof(RenderMesh))]
    [ExcludeComponent (typeof(Disabled))]
    private struct SetQuadrantDataJob : IJobForEachWithEntity<Translation, Planetoid>
    {
        public NativeMultiHashMap<int, EntityWithProps>.Concurrent quadrantMultiHashMap;

        public void Execute(Entity entity, int index, ref Translation translation, ref Planetoid planetoidCrashData)
        {
            int hashMapKey = GetPositionHashMapKey(translation.Value);
            quadrantMultiHashMap.Add(hashMapKey, new EntityWithProps
            {
                entity = entity,
                position = translation.Value,
                vecSqrMag = translation.Value.x * translation.Value.x + translation.Value.y * translation.Value.y,
                crashTime = 0,
                planetoidCrashData = planetoidCrashData
            });
        }
    }

    //private EndSimulationEntityCommandBufferSystem endSimulationEntityCommandBufferSystem;
    public static NativeMultiHashMap<int, EntityWithProps> quadrantMultiHashMap;
    protected override void OnCreate()
    {
        //endSimulationEntityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        quadrantMultiHashMap = new NativeMultiHashMap<int, EntityWithProps>(0, Allocator.Persistent);
        base.OnCreate();
    }

    protected override void OnDestroy()
    {
        quadrantMultiHashMap.Dispose();
        base.OnDestroy();
    }

    protected override void OnUpdate()
    {
        EntityQuery entityQuery = GetEntityQuery(typeof(Translation), typeof(Planetoid));

        quadrantMultiHashMap.Clear();

        if (quadrantMultiHashMap.Capacity < entityQuery.CalculateLength())
        {
            quadrantMultiHashMap.Capacity = entityQuery.CalculateLength();
        }

        SetQuadrantDataJob setQuadrantDataJob = new SetQuadrantDataJob
        {
            quadrantMultiHashMap = quadrantMultiHashMap.ToConcurrent()
        };

        //JobHandle jobHandle = setQuadrantDataJob.Schedule(this);
        JobHandle jobHandle = JobForEachExtensions.Schedule(setQuadrantDataJob, entityQuery); //extension for jobs using parallel for
        jobHandle.Complete();

        DebugDrawQuadrant(CodeMonkey.Utils.UtilsClass.GetMouseWorldPosition());
    }
}
