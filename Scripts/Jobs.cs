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

namespace Segments
{

    // [Unity.Burst.BurstCompile]
    // struct SetupSubmeshJob : IJob
    // {
    //     public Mesh.MeshData meshData;
    //     public int numIndices;
    //     void IJob.Execute ()
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
        [WriteOnly] public NativeSlice<uint> Dst;
        void IJobParallelForBatch.Execute ( int startIndex , int count )
        {
            int max = startIndex + count;
            for( int i=startIndex ; i<max ; i++ )
                Dst[i] = (uint) i;
        }
    }

    [Unity.Burst.BurstCompile]
    struct NativeCopyJob<T> : IJob where T : unmanaged
    {
        [ReadOnly] public NativeSlice<T> Src;
        [WriteOnly] public NativeSlice<T> Dst;
        void IJob.Execute () => Dst.CopyFrom( Src );
    }

    [Unity.Burst.BurstCompile]
    struct BoundsJob : IJob
    {
        [ReadOnly] public NativeSlice<float3x2> Segments;
        [NativeDisableContainerSafetyRestriction][WriteOnly] public NativeSlice<AABB> Bounds;
        void IJob.Execute ()
        {
            MinMaxAABB combined = MinMaxAABB.Empty;
            for( int i=Segments.Length-1 ; i!=-1 ; i-- )
                combined.Encapsulate( new MinMaxAABB{ Min=Segments[i].c0 , Max=Segments[i].c1 } );
            Bounds[0] = new Bounds{ min=combined.Min , max=combined.Max }.ToAABB();
        }
    }

    // partial struct PushMeshDataJob : IJob
    // {
    //     [ReadOnly] public Mesh.MeshDataArray MeshDataArray;
    //     public Mesh MeshObject;
    //     void IJob.Execute ()
    //     {
    //         Mesh.ApplyAndDisposeWritableMeshData(
    //             data: MeshDataArray ,
    //             mesh: MeshObject ,
    //             flags: MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds
    //         );
    //         // mesh.UploadMeshData( false );
    //     }
    // }

}
