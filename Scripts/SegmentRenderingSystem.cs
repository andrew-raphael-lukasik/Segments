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
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}

		public void OnDestroy ( ref SystemState state )
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}

		public void OnUpdate ( ref SystemState state )
		{
			var initializationSystem = state.World.GetExistingSystemManaged<SegmentInitializationSystem>();
			if( initializationSystem.numBatchesToPush==0 ) return;

			var batches = Core.Batches;
			int numBatches = initializationSystem.numBatchesToPush;

			JobHandle.CompleteAll( initializationSystem.DefferedBoundsJobs.AsArray() );

			// push bounds:
			____push_bounds.Begin();
			for( int i=numBatches-1 ; i!=-1 ; i-- )
				if( i<initializationSystem.DefferedBounds.Length )
					batches[i].mesh.bounds = initializationSystem.DefferedBounds[i];
			____push_bounds.End();

			JobHandle.CompleteAll( initializationSystem.FillMeshDataArrayJobs.AsArray() );

			// push mesh data:
			____push_mesh_data.Begin();
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				var mesh = batch.mesh;
				Mesh.ApplyAndDisposeWritableMeshData(
					initializationSystem.MeshDataArrays[i] ,
					mesh ,
					MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds
				);
				mesh.UploadMeshData( false );
			}
			____push_mesh_data.End();

			initializationSystem.numBatchesToPush = 0;
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
