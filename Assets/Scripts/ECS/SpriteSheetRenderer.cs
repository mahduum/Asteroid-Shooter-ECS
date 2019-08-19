using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateAfter(typeof(SpriteSheetAnimationSystem))]
public class SpriteSheetRenderer : ComponentSystem
{
   protected override void OnUpdate()
    {
        MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
        Vector4[] uv = new Vector4[1];
        Entities.WithNone<Planetoid>().ForEach((ref Translation translation, ref SpriteSheetAnimation_Data spriteSheetAnimation_Data) =>
        {
            /* calculation moved to job
            int uvOffsetXDivisor = spriteSheetAnimation_Data.currentFrame % 8;
            int uvOffsetYDivisor = (int) math.floor((spriteSheetAnimation_Data.frameCount - spriteSheetAnimation_Data.currentFrame) / 8);
            float uvWidth = 1f / 8; //columns
            float uvHeight = 1f / 6; //rows
            float uvOffsetX = uvWidth * uvOffsetXDivisor;
            float uvOffsetY = uvHeight * uvOffsetYDivisor;
            Vector4 uv = new Vector4(uvWidth, uvHeight, uvOffsetX, uvOffsetY);
            */
            uv[0] = spriteSheetAnimation_Data.uv;
            materialPropertyBlock.SetVectorArray("_MainTex_UV", uv);//new Vector4[] { spriteSheetAnimation_Data.uv });

            Graphics.DrawMesh(
                GameHandler.meshData.explosionMesh,
                //translation.Value,
                //Quaternion.identity,
                //are replaced by matrix which will is also necessary for DrawInstanced:
                spriteSheetAnimation_Data.matrix,
                GameHandler.meshData.explosionMaterial,
                0,//Layer
                Camera.main,
                0,//Submesh
                materialPropertyBlock
                );
        });
    }
}
