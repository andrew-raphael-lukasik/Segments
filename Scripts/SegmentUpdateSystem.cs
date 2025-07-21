using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;

namespace Segments
{
    [WorldSystemFilter( WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor )]
    [UpdateInGroup( typeof(PresentationSystemGroup) )]
    [UpdateAfter( typeof(EntitiesGraphicsSystem) )]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    internal partial struct SegmentUpdateSystem : ISystem
    {
        static readonly ProfilerMarker
            ___allocate_writable_mesh_data = new ProfilerMarker(nameof(___allocate_writable_mesh_data).TrimStart('_')) ,
            ___set_vertex_buffer_params = new ProfilerMarker(nameof(___set_vertex_buffer_params).TrimStart('_')) ,
            ___schedule_copy_buffer_jobs = new ProfilerMarker(nameof(___schedule_copy_buffer_jobs).TrimStart('_')) ,
            ___set_index_buffer_params = new ProfilerMarker(nameof(___set_index_buffer_params).TrimStart('_')) ,
            ___get_mesh = new ProfilerMarker(nameof(___get_mesh).TrimStart('_')) ,
            ___set_sub_mesh = new ProfilerMarker(nameof(___set_sub_mesh).TrimStart('_')) ,
            ___push_bounds = new ProfilerMarker(nameof(___push_bounds).TrimStart('_')) ,
            ___push_mesh_data = new ProfilerMarker(nameof(___push_mesh_data).TrimStart('_'));

        NativeArray<uint> _predefinedIndexBuffer;
        NativeList<( Mesh.MeshDataArray meshDataArray , Mesh.MeshData meshData , int numVertices , JobHandle boundsJobHandle , JobHandle copyVerticesJobHandle , JobHandle copyIndicesJobHandle )> _midUpdateData;
        EntityQuery _query;

        [Unity.Burst.BurstCompile]
        public void OnCreate ( ref SystemState state )
        {
            _predefinedIndexBuffer = new ( 128_000 , Allocator.Persistent );
            var job = new PredefinedIndicesJob{
                Dst = _predefinedIndexBuffer ,
            };
            JobHandle jobHandle = job.Schedule( arrayLength:_predefinedIndexBuffer.Length , indicesPerJobCount:_predefinedIndexBuffer.Length/128 );
            jobHandle.Complete();
            // for( uint i=0 ; i<128_000 ; i++ ) _predefinedIndexBuffer[(int)i] = i;

            _midUpdateData = new( Allocator.Persistent );
            _query = state.GetEntityQuery( new NativeList<ComponentType>(1,Allocator.Temp){ ComponentType.ReadWrite<Segment>() , ComponentType.ReadWrite<MaterialMeshInfo>() , ComponentType.ReadWrite<RenderBounds>() }.AsArray() );
        }

        [Unity.Burst.BurstCompile]
        public void OnDestroy ( ref SystemState state )
        {
            if( _predefinedIndexBuffer.IsCreated ) _predefinedIndexBuffer.Dispose();
            if( _midUpdateData.IsCreated ) _midUpdateData.Dispose();
        }

        //[Unity.Burst.BurstCompile]
        public void OnUpdate ( ref SystemState state )
        {
            int numEntities = _query.CalculateEntityCount();
            var segmentBufferLookup = state.GetBufferLookup<Segment>( isReadOnly:true );
            NativeArray<AABB> bounds = new ( numEntities , Allocator.TempJob );
            _midUpdateData.Clear();
            int i = 0;

            foreach( var ( _ , entity ) in SystemAPI
                .Query< RefRO<MaterialMeshInfo> >()
                .WithAll<RenderBounds,Segment>()
                .WithChangeFilter<Segment>()
                .WithEntityAccess()
            )
            {
                ___allocate_writable_mesh_data.Begin();
                Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
                Mesh.MeshData meshData = meshDataArray[0];
                ___allocate_writable_mesh_data.End();
                
                var segmentBuffer = segmentBufferLookup[entity];
                int numSegments = segmentBuffer.Length;
                int numVertices = numSegments * 2;
                
                ___set_vertex_buffer_params.Begin();
                meshData.SetVertexBufferParams( numVertices , new VertexAttributeDescriptor(VertexAttribute.Position) );
                ___set_vertex_buffer_params.End();
                
                ___set_index_buffer_params.Begin();
                meshData.SetIndexBufferParams( numVertices , IndexFormat.UInt32 );
                ___set_index_buffer_params.End();

                ___schedule_copy_buffer_jobs.Begin();
                var segmentBufferAsFloat3x2Array = segmentBuffer.AsNativeArray().Reinterpret<float3x2>();
                var boundsJobHandle = new BoundsJob{
                    Segments = segmentBufferAsFloat3x2Array ,
                    Bounds = bounds.Slice(i,1) ,
                }.Schedule();
                var vertexData = meshData.GetVertexData<float3x2>();
                var indexData = meshData.GetIndexData<uint>().Slice( 0 , numVertices );
                JobHandle copyIndicesJobHandle = new NativeCopyJob<uint>{
                    Src = _predefinedIndexBuffer.Slice( 0 , numVertices ) ,
                    Dst = indexData ,
                }.Schedule();
                JobHandle copyVerticesJobHandle = new NativeCopyJob<float3x2>{
                    Src = segmentBufferAsFloat3x2Array ,
                    Dst = vertexData ,
                }.Schedule( copyIndicesJobHandle );
                ___schedule_copy_buffer_jobs.End();

                _midUpdateData.Add( ( meshDataArray , meshData , numVertices , boundsJobHandle , copyVerticesJobHandle , copyIndicesJobHandle ) );

                i++;
            }

            var graphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            i = 0;

            foreach( var ( materialMeshInfo , renderBounds, entity ) in SystemAPI
                    .Query< RefRO<MaterialMeshInfo> , RefRW<RenderBounds> >()
                    .WithEntityAccess()
                    .WithAll<Segment>()
                    .WithChangeFilter<Segment>()
            )
            {
                var next = _midUpdateData[i];

                next.copyVerticesJobHandle.Complete();
                next.copyIndicesJobHandle.Complete();

                ___set_sub_mesh.Begin();
                var meshData = next.meshData;
                meshData.subMeshCount = 1;
                meshData.SetSubMesh( 0 , new SubMeshDescriptor(0,next.numVertices,MeshTopology.Lines) , MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds );
                ___set_sub_mesh.End();

                ___get_mesh.Begin();
                Mesh mesh = graphicsSystem.GetMesh( materialMeshInfo.ValueRO.MeshID );
                ___get_mesh.End();

                if( mesh==null )
                {
                    Debug.LogError($"{entity} MESH JEST NULL, materialMeshInfo.MeshID: {materialMeshInfo.ValueRO.MeshID.value}");
                }

                ___push_mesh_data.Begin();
                Mesh.ApplyAndDisposeWritableMeshData( next.meshDataArray , mesh , MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds );
                ___push_mesh_data.End();
                
                ___push_bounds.Begin();
                next.boundsJobHandle.Complete();
                renderBounds.ValueRW.Value = bounds[i];
                ___push_bounds.End();

                i++;
            }
        }

    }
}
