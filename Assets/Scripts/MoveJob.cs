using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;

public struct MoveJob : IJobParallelForTransform
{
    [ReadOnly]
    public NativeArray<float> speeds;
    [ReadOnly]
    public NativeArray<Vector3> directions;

    public void Execute (int index, TransformAccess transform)
    {
        transform.position += speeds[index] * directions[index];
    }
}
