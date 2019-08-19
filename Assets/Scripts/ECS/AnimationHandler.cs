using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;

public class AnimationHandler : ComponentSystem
{
    private EntityManager entityManager;
    private EntityArchetype entityArchetype;
    public static NativeQueue<float3> explosionCoordsQueue;
    protected override void OnCreate()
    {
        entityManager = World.Active.EntityManager;
        explosionCoordsQueue = new NativeQueue<float3>(Allocator.Persistent);
        entityArchetype = entityManager.CreateArchetype(typeof(Translation), typeof(Scale), typeof(SpriteSheetAnimation_Data));
    }
    protected override void OnDestroy()
    {
        explosionCoordsQueue.Dispose();
        base.OnDestroy();
    }
    protected override void OnUpdate()
    {
        while (explosionCoordsQueue.TryDequeue(out float3 explosionPoint))
        {
            Entity explosionEntity = entityManager.CreateEntity(entityArchetype);
            entityManager.SetComponentData(explosionEntity, new Translation { Value = explosionPoint });
            entityManager.SetComponentData(explosionEntity, new Scale { Value = 4f });
            entityManager.SetComponentData(explosionEntity, new SpriteSheetAnimation_Data
            {
                currentFrame = 0,
                frameCount = 47,
                frameTimer = 0,
                frameTimerMax = 0.05f
            });
        }
    }

}
