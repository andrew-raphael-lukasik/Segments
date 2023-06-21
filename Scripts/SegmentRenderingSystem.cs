using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using Unity.Entities;
using Unity.Jobs;

using BurstCompile = Unity.Burst.BurstCompileAttribute;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(PresentationSystemGroup) )]
	[BurstCompile]
	internal partial struct SegmentRenderingSystem : ISystem
	{
		static readonly ProfilerMarker
            ____push_bounds = new ProfilerMarker("push_bounds") ,
            ____push_mesh_data = new ProfilerMarker("push_mesh_data");

		public void OnCreate ( ref SystemState state )
		{
			state.RequireForUpdate<Singleton>();
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		public void OnDestroy ( ref SystemState state )
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		public void OnUpdate ( ref SystemState state )
		{
			var systemData = state.EntityManager.GetSharedComponentManaged<SegmentsSharedData>( SystemAPI.GetSingletonEntity<Singleton>() );
			int numBatches = systemData.NumBatchesToPush[0];
			if( numBatches==0 ) return;
			
			JobHandle.CompleteAll( systemData.DeferredBoundsJobs.AsArray() );

			// push bounds:
			var batches = Core.Batches;
			____push_bounds.Begin();
			for( int i=numBatches-1 ; i!=-1 ; i-- )
				if( i<systemData.DeferredBounds.Length )
					batches[i].mesh.bounds = systemData.DeferredBounds[i];
			____push_bounds.End();

			JobHandle.CompleteAll( systemData.FillMeshDataArrayJobs.AsArray() );

			// push mesh data:
			____push_mesh_data.Begin();
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				var mesh = batch.mesh;
				var data = systemData.MeshDataArrays[i];
				Mesh.ApplyAndDisposeWritableMeshData(
					data: data ,
					mesh: mesh ,
					flags: MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds
				);
				mesh.UploadMeshData( false );
			}
			____push_mesh_data.End();

			systemData.NumBatchesToPush[0] = 0;
		}

		void OnBeginCameraRendering ( ScriptableRenderContext context , Camera camera )
		{
			#if UNITY_EDITOR
			if( camera.name=="Preview Scene Camera" ) return;
			#endif

			Core.Render( camera );
		}

	}
}
