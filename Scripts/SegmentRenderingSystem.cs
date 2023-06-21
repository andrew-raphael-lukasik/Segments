using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(PresentationSystemGroup) )]
	[Unity.Burst.BurstCompile]
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
			Entity singleton = SystemAPI.GetSingletonEntity<Singleton>();
			var meshDataArrays = SystemAPI.GetBuffer<MeshDataArrayElement>( singleton );
			var deferredBounds = SystemAPI.GetBuffer<DeferredBoundsElement>( singleton );
			var deferredBoundsJobs = SystemAPI.GetBuffer<DeferredBoundsJobsElement>( singleton );
			var fillMeshDataArrayJobs = SystemAPI.GetBuffer<FillMeshDataArrayJobsElement>( singleton );
			var numBatchesToPush = SystemAPI.GetBuffer<NumBatchesToPushElement>( singleton );

			int numBatches = numBatchesToPush[0].Value;
			if( numBatches==0 ) return;
			
			JobHandle.CompleteAll( deferredBoundsJobs.Reinterpret<JobHandle>().ToNativeArray(Allocator.Temp) );

			// push bounds:
			var batches = Core.Batches;
			____push_bounds.Begin();
			for( int i=numBatches-1 ; i!=-1 ; i-- )
				if( i<deferredBounds.Length )
					batches[i].mesh.bounds = deferredBounds[i].Value;
			____push_bounds.End();

			JobHandle.CompleteAll( fillMeshDataArrayJobs.Reinterpret<JobHandle>().ToNativeArray(Allocator.Temp) );

			// push mesh data:
			____push_mesh_data.Begin();
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				var mesh = batch.mesh;
				var data = meshDataArrays[i].Value;
				Mesh.ApplyAndDisposeWritableMeshData(
					data: data ,
					mesh: mesh ,
					flags: MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds
				);
				mesh.UploadMeshData( false );
			}
			____push_mesh_data.End();

			numBatchesToPush[0] = new NumBatchesToPushElement{ Value=0 };
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
