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
	[Unity.Burst.BurstCompile]
	internal partial struct SegmentRenderingSystem : ISystem
	{
		static readonly ProfilerMarker
			___allocate_writable_mesh_data = new ProfilerMarker("allocate_writable_mesh_data") ,
			___set_vertex_buffer_params = new ProfilerMarker("set_vertex_buffer_params") ,
			___copy_segment_buffer_data = new ProfilerMarker("copy_segment_buffer_data") ,
			___set_index_buffer_params = new ProfilerMarker("set_index_buffer_params") ,
			___set_sub_mesh = new ProfilerMarker("set_sub_mesh") ,
            ____push_bounds = new ProfilerMarker("push_bounds") ,
            ____push_mesh_data = new ProfilerMarker("push_mesh_data");

		[Unity.Burst.BurstCompile]
		public void OnCreate ( ref SystemState state ) {}

		[Unity.Burst.BurstCompile]
		public void OnDestroy ( ref SystemState state ) {}

		//[Unity.Burst.BurstCompile]
		public void OnUpdate ( ref SystemState state )
		{
			foreach(
				var ( segmentBuffer , renderMeshArray , materialMeshInfo , renderBounds ) in
				SystemAPI.Query< DynamicBuffer<Segment> , RenderMeshArray , MaterialMeshInfo , RefRW<RenderBounds> >()
				.WithChangeFilter<Segment>()
			)
			{
				___allocate_writable_mesh_data.Begin();
				var dataArray = Mesh.AllocateWritableMeshData(1);
				var data = dataArray[0];
				___allocate_writable_mesh_data.End();
				
				int numSegments = segmentBuffer.Length;
				int numVertices = numSegments * 2;
				
				___set_vertex_buffer_params.Begin();
				data.SetVertexBufferParams( numVertices , new VertexAttributeDescriptor(VertexAttribute.Position) );
				___set_vertex_buffer_params.End();

				___copy_segment_buffer_data.Begin();
				{
					var src = segmentBuffer.Reinterpret<float3x2>().AsNativeArray();
					var dst = data.GetVertexData<float3x2>();
					src.CopyTo( dst );
				}
				___copy_segment_buffer_data.End();
				
				___set_index_buffer_params.Begin();
				data.SetIndexBufferParams( numVertices , IndexFormat.UInt32 );
				var indexBuffer = data.GetIndexData<uint>();
				for( uint i=0 ; i<numVertices ; ++i )
					indexBuffer[ (int) i ] = i;
				___set_index_buffer_params.End();

				___set_sub_mesh.Begin();
				data.subMeshCount = 1;
				data.SetSubMesh( 0 , new SubMeshDescriptor(0,numVertices,MeshTopology.Lines) );
				___set_sub_mesh.End();

				var mesh = renderMeshArray.GetMesh( materialMeshInfo );
				
				____push_mesh_data.Begin();
				Mesh.ApplyAndDisposeWritableMeshData( dataArray , mesh );
				____push_mesh_data.End();
				
				____push_bounds.Begin();
				mesh.RecalculateBounds();
				renderBounds.ValueRW.Value = mesh.bounds.ToAABB();
				____push_bounds.End();
			}
		}

	}
}
