using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Jobs;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Transforms;

public struct SpriteSheetAnimation_Data : IComponentData
{
    public int currentFrame;
    public int frameCount;
    public float frameTimer;
    public float frameTimerMax;

    public Vector4 uv;
    public Matrix4x4 matrix;

}
[BurstCompile]
public class SpriteSheetAnimationSystem : JobComponentSystem
{
    public struct Job : IJobForEach<SpriteSheetAnimation_Data, Translation>
    {
        public float deltaTime;

        public void Execute (ref SpriteSheetAnimation_Data spriteSheetAnimation_Data, ref Translation translation)
        {
            spriteSheetAnimation_Data.frameTimer += deltaTime;

            while(spriteSheetAnimation_Data.frameTimer >= spriteSheetAnimation_Data.frameTimerMax && spriteSheetAnimation_Data.currentFrame < spriteSheetAnimation_Data.frameCount)//while because it can be much bigger so subtracting once may not be enough
            {
                spriteSheetAnimation_Data.frameTimer -= spriteSheetAnimation_Data.frameTimerMax; //TODO check if it's enough to set it to zero
                spriteSheetAnimation_Data.currentFrame = spriteSheetAnimation_Data.currentFrame + 1;

                int uvOffsetXDivisor = spriteSheetAnimation_Data.currentFrame % 8;
                int uvOffsetYDivisor = (int)math.floor((spriteSheetAnimation_Data.frameCount - spriteSheetAnimation_Data.currentFrame) / 8);
                float uvWidth = 1f / 8; //columns
                float uvHeight = 1f / 6; //rows
                float uvOffsetX = uvWidth * uvOffsetXDivisor;
                float uvOffsetY = uvHeight * uvOffsetYDivisor;
                spriteSheetAnimation_Data.uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);
                
                float3 position = translation.Value;
                position.z = position.y * 0.01f;
                spriteSheetAnimation_Data.matrix = Matrix4x4.TRS(translation.Value, Quaternion.identity, Vector3.one);
            }
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        Job job = new Job
        {
            deltaTime = Time.deltaTime
        };
        JobHandle jobHandle = job.Schedule(this, inputDeps);
        return jobHandle;
    }
}