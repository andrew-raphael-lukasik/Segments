using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using Segments.Jobs;

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
        NativeList<JobHandle> _jobHandles1, _jobHandles2;
        NativeList<NativeList<AABB>> _aabbBuffers;
        NativeList<( Mesh.MeshDataArray meshDataArray , Mesh.MeshData meshData , int numVertices , JobHandle boundsJobHandle , JobHandle copyVerticesJobHandle , JobHandle copyIndicesJobHandle )> _midUpdateData;
        EntityQuery _query;

        [Unity.Burst.BurstCompile]
        public void OnCreate ( ref SystemState state )
        {
            _predefinedIndexBuffer = new ( 128_000 , Allocator.Persistent );
            var job = new PredefinedIndicesJob{
                dst = _predefinedIndexBuffer ,
            };
            JobHandle jobHandle = job.Schedule( arrayLength:_predefinedIndexBuffer.Length , indicesPerJobCount:_predefinedIndexBuffer.Length/128 );
            jobHandle.Complete();
            // for( uint i=0 ; i<128_000 ; i++ ) _predefinedIndexBuffer[(int)i] = i;

            _jobHandles1 = new (16, Allocator.Persistent);
            _jobHandles2 = new (16, Allocator.Persistent);
            _aabbBuffers = new (16, Allocator.Persistent);
            for( int i=0 ; i<16 ; i++ )
                _aabbBuffers.Add(new (16, Allocator.Persistent));

            _midUpdateData = new( Allocator.Persistent );
            _query = state.GetEntityQuery( new NativeList<ComponentType>(3,Allocator.Temp){
                ComponentType.ReadOnly<Segment>() ,
                ComponentType.ReadOnly<SegmentUpdateRequest>() ,
                ComponentType.ReadOnly<MaterialMeshInfo>() ,
                ComponentType.ReadWrite<RenderBounds>() ,
            }.AsArray() );

            state.RequireForUpdate(_query);
        }

        [Unity.Burst.BurstCompile]
        public void OnDestroy ( ref SystemState state )
        {
            if( _predefinedIndexBuffer.IsCreated ) _predefinedIndexBuffer.Dispose();
            if( _jobHandles1.IsCreated ) _jobHandles1.Dispose();
            if( _jobHandles2.IsCreated ) _jobHandles2.Dispose();
            if( _aabbBuffers.IsCreated )
            {
                foreach( var list in _aabbBuffers )
                    if( list.IsCreated ) list.Dispose();
                _aabbBuffers.Dispose();
            }
            if( _midUpdateData.IsCreated ) _midUpdateData.Dispose();
        }

        //[Unity.Burst.BurstCompile]
        public void OnUpdate ( ref SystemState state )
        {
            int numEntities = _query.CalculateEntityCount();
            NativeArray<AABB> bounds = new ( numEntities , Allocator.TempJob );
            _midUpdateData.Clear();
            int i = 0;

            foreach( var ( segment , materialMeshInfo , renderBounds , entity ) in SystemAPI
                .Query< RefRO<Segment> , RefRO<MaterialMeshInfo> , RefRW<RenderBounds> >()
                .WithAll<SegmentUpdateRequest>()
                .WithEntityAccess()
            )
            {
                ___allocate_writable_mesh_data.Begin();
                Mesh.MeshDataArray meshDataArray = Mesh.AllocateWritableMeshData(1);
                Mesh.MeshData meshData = meshDataArray[0];
                ___allocate_writable_mesh_data.End();
                
                var segmentBuffer = segment.ValueRO.Buffer;
                int numSegments = segmentBuffer.Length;
                int numVertices = numSegments * 2;

                // upsize index buffer when necessary
                if( numVertices>_predefinedIndexBuffer.Length )
                {
                    Debug.LogWarning($"_predefinedIndexBuffer upsized to {_predefinedIndexBuffer.Length}");
                    foreach( var item in _midUpdateData )
                        item.copyIndicesJobHandle.Complete();
                    _predefinedIndexBuffer.Dispose();
                    _predefinedIndexBuffer = new ( numVertices , Allocator.Persistent );
                }
                
                ___set_vertex_buffer_params.Begin();
                meshData.SetVertexBufferParams( numVertices , new VertexAttributeDescriptor(VertexAttribute.Position) );
                ___set_vertex_buffer_params.End();
                
                ___set_index_buffer_params.Begin();
                meshData.SetIndexBufferParams( numVertices , IndexFormat.UInt32 );
                ___set_index_buffer_params.End();

                ___schedule_copy_buffer_jobs.Begin();
                JobHandle boundsJobHandle, copyVerticesJobHandle, copyIndicesJobHandle;
                var segmentBufferAsFloat3x2Array = segmentBuffer.AsArray();
                {
                    var vertexData = meshData.GetVertexData<float3x2>();
                    var indexData = meshData.GetIndexData<uint>().Slice(0, numVertices);

                    const int dispatchSize = 1<<14;
                    if( numSegments<=dispatchSize )
                    {
                        boundsJobHandle = new BoundsJob{
                            input = segmentBufferAsFloat3x2Array ,
                            output = bounds.Slice(i,1) ,
                        }.Schedule();

                        copyIndicesJobHandle = new NativeCopyJob<uint>{
                            src = _predefinedIndexBuffer.Slice(0, numVertices),
                            dst = indexData,
                        }.Schedule();
                        copyVerticesJobHandle = new NativeCopyJob<float3x2>{
                            src = segmentBufferAsFloat3x2Array,
                            dst = vertexData,
                        }.Schedule(copyIndicesJobHandle);
                    }
                    else
                    {
                        {
                            int numDispatches = numSegments/dispatchSize + math.min(numSegments%dispatchSize, 1);
                            int numSegmentsPerDispatch = numSegments / numDispatches;
                            _jobHandles1.Length = numDispatches;
                            NativeArray<AABB> dResults;
                            {
                                if( _aabbBuffers.Length==i )
                                    _aabbBuffers.Add(new (16, Allocator.Persistent));

                                var list = _aabbBuffers[i];
                                list.Length = numDispatches;
                                dResults = list.AsArray();
                            }
                            int dLast = numDispatches-1;
                            for (int d=0; d<dLast; d++)
                            {
                                _jobHandles1[d] = new BoundsJob{
                                    input = segmentBufferAsFloat3x2Array.Slice(d*numSegmentsPerDispatch, numSegmentsPerDispatch),
                                    output = dResults.Slice(d,1),
                                }.Schedule();
                            }
                            int lastDispStart = dLast*numSegmentsPerDispatch;
                            _jobHandles1[dLast] = new BoundsJob{
                                input = segmentBufferAsFloat3x2Array.Slice(lastDispStart, numSegments-lastDispStart),
                                output = dResults.Slice(dLast,1),
                            }.Schedule();
                            boundsJobHandle = new BoundsCombineJob{
                                input = dResults,
                                output = bounds.Slice(i,1),
                            }.Schedule(JobHandle.CombineDependencies(_jobHandles1.AsArray()));
                        }
                        {
                            int numDispatches = numVertices/dispatchSize + math.min(numVertices%dispatchSize, 1);
                            int numVerticesPerDispatch = numVertices / numDispatches;
                            int numSegmentsPerDispatch = numSegments / numDispatches;
                            NativeArray<JobHandle> copyIndicesJobHandles;
                            {
                                _jobHandles1.Length = numDispatches;
                                copyIndicesJobHandles = _jobHandles1.AsArray();
                            }
                            NativeArray<JobHandle> copyVerticesJobHandles;
                            {
                                _jobHandles2.Length = numDispatches;
                                copyVerticesJobHandles = _jobHandles2.AsArray();
                            }
                            int dLast = numDispatches-1;
                            for (int d=0; d<dLast; d++)
                            {
                                int div = d*numVerticesPerDispatch;
                                int dis = d*numSegmentsPerDispatch;
                                copyIndicesJobHandles[d] = new NativeCopyNoSafetyChecksJob<uint>{
                                    src = _predefinedIndexBuffer.Slice(div, numVerticesPerDispatch),
                                    dst = indexData.Slice(div, numVerticesPerDispatch),
                                }.Schedule();
                                copyVerticesJobHandles[d] = new NativeCopyNoSafetyChecksJob<float3x2>{
                                    src = segmentBufferAsFloat3x2Array.Slice(dis, numSegmentsPerDispatch),
                                    dst = vertexData.Slice(dis, numSegmentsPerDispatch),
                                }.Schedule(copyIndicesJobHandles[d]);
                            }
                            int lastVertexDispStart = dLast*numVerticesPerDispatch;
                            int lastSegmentDispStart = dLast*numSegmentsPerDispatch;
                            copyIndicesJobHandles[dLast] = new NativeCopyNoSafetyChecksJob<uint>{
                                src = _predefinedIndexBuffer.Slice(lastVertexDispStart, numVertices-lastVertexDispStart),
                                dst = indexData.Slice(lastVertexDispStart, numVertices-lastVertexDispStart),
                            }.Schedule();
                            copyVerticesJobHandles[dLast] = new NativeCopyNoSafetyChecksJob<float3x2>{
                                src = segmentBufferAsFloat3x2Array.Slice(lastSegmentDispStart, numSegments-lastSegmentDispStart),
                                dst = vertexData.Slice(lastSegmentDispStart, numSegments-lastSegmentDispStart),
                            }.Schedule(copyIndicesJobHandles[dLast]);

                            copyVerticesJobHandle = JobHandle.CombineDependencies(copyVerticesJobHandles);
                            copyIndicesJobHandle = JobHandle.CombineDependencies(copyIndicesJobHandles);
                        }
                    }
                }
                ___schedule_copy_buffer_jobs.End();

                _midUpdateData.Add( ( meshDataArray , meshData , numVertices , boundsJobHandle , copyVerticesJobHandle , copyIndicesJobHandle ) );

                i++;
            }

            var graphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            i = 0;

            foreach( var ( segment , materialMeshInfo , renderBounds , entity ) in SystemAPI
                .Query< RefRO<Segment> , RefRO<MaterialMeshInfo> , RefRW<RenderBounds> >()
                .WithAll<SegmentUpdateRequest>()
                .WithEntityAccess()
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

                ___push_mesh_data.Begin();
                Mesh.ApplyAndDisposeWritableMeshData( next.meshDataArray , mesh , MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds | MeshUpdateFlags.DontResetBoneBounds );
                ___push_mesh_data.End();
                
                ___push_bounds.Begin();
                next.boundsJobHandle.Complete();
                renderBounds.ValueRW.Value = bounds[i];
                ___push_bounds.End();

                i++;

                // flag update request as fulfilled:
                state.EntityManager.SetComponentEnabled<SegmentUpdateRequest>(entity, false);
            }
        }

    }
}
