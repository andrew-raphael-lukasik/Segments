using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Rendering;
using Unity.Jobs;

namespace Segments.Jobs
{

    // [Unity.Burst.BurstCompile]
    // struct SetupSubmeshJob : IJob
    // {
    //     public Mesh.MeshData meshData;
    //     public int numIndices;
    //     void IJob.Execute()
    //     {
    //         meshData.subMeshCount = 1;
    //         meshData.SetSubMesh(
    //             index:  0 ,
    //             desc:   new SubMeshDescriptor( indexStart:0 , indexCount:numIndices , topology:MeshTopology.Lines ) ,
    //             flags:  MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds
    //         );
    //     }
    // }

    [Unity.Burst.BurstCompile]
    struct PredefinedIndicesJob : IJobParallelForBatch
    {
        [WriteOnly] public NativeSlice<uint> dst;
        void IJobParallelForBatch.Execute(int startIndex, int count)
        {
            int max = startIndex + count;
            for( int i=startIndex ; i<max ; i++ )
                dst[i] = (uint) i;
        }
    }

    [Unity.Burst.BurstCompile]
    struct NativeCopyJob<T> : IJob where T : unmanaged
    {
        [ReadOnly] public NativeSlice<T> src;
        [WriteOnly] public NativeSlice<T> dst;
        void IJob.Execute() => dst.CopyFrom(src);
    }

    [Unity.Burst.BurstCompile]
    struct NativeCopyNoSafetyChecksJob<T> : IJob where T : unmanaged
    {
        [NativeDisableContainerSafetyRestriction][ReadOnly] public NativeSlice<T> src;
        [NativeDisableContainerSafetyRestriction][WriteOnly] public NativeSlice<T> dst;
        void IJob.Execute() => dst.CopyFrom(src);
    }

    [Unity.Burst.BurstCompile]
    struct BoundsJob : IJob
    {
        [ReadOnly] public NativeSlice<float3x2> input;
        [NativeDisableContainerSafetyRestriction][WriteOnly] public NativeSlice<AABB> output;
        void IJob.Execute()
        {
            var minmax = MinMaxAABB.Empty;
            foreach(var pair in input) {
                minmax.Encapsulate(pair.c0);
                minmax.Encapsulate(pair.c1);
            }
            output[0] = new Bounds{min=minmax.Min, max=minmax.Max}.ToAABB();
        }
    }

    [Unity.Burst.BurstCompile]
    struct BoundsCombineJob : IJob
    {
        [ReadOnly] public NativeSlice<AABB> input;
        [NativeDisableContainerSafetyRestriction][WriteOnly] public NativeSlice<AABB> output;
        void IJob.Execute()
        {
            var minmax = MinMaxAABB.Empty;
            foreach(var next in input)
                minmax.Encapsulate(next);
            output[0] = new Bounds{min=minmax.Min, max=minmax.Max}.ToAABB();
        }
    }

    // struct PushMeshDataJob : IJob
    // {
    //     [ReadOnly] public Mesh.MeshDataArray meshDataArray;
    //     public Mesh meshObject;
    //     void IJob.Execute()
    //     {
    //         Mesh.ApplyAndDisposeWritableMeshData(
    //             data: meshDataArray ,
    //             mesh: meshObject ,
    //             flags: MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds
    //         );
    //         // mesh.UploadMeshData( false );
    //     }
    // }

}
