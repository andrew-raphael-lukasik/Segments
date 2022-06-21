using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using Unity.Entities;
using Unity.Jobs;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(PresentationSystemGroup) )]
	internal partial class SegmentRenderingSystem : SystemBase
	{


		SegmentInitializationSystem _initializationSystem;


		protected override void OnCreate ()
		{
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
			_initializationSystem = World.GetExistingSystem<SegmentInitializationSystem>();
		}


		protected override void OnDestroy ()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}


		protected override void OnUpdate ()
		{
			if( _initializationSystem.numBatchesToPush==0 ) return;

			var batches = Core.Batches;
			int numBatches = _initializationSystem.numBatchesToPush;

			JobHandle.CompleteAll( _initializationSystem.DefferedBoundsJobs );

			// push bounds:
			Profiler.BeginSample("push_bounds");
			for( int i=numBatches-1 ; i!=-1 ; i-- )
				if( i<_initializationSystem.DefferedBounds.Length )
					batches[i].mesh.bounds = _initializationSystem.DefferedBounds[i];
			Profiler.EndSample();

			JobHandle.CompleteAll( _initializationSystem.FillMeshDataArrayJobs );

			// push mesh data:
			Profiler.BeginSample("push_mesh_data");
			for( int i=numBatches-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				var mesh = batch.mesh;
				Mesh.ApplyAndDisposeWritableMeshData(
					_initializationSystem.MeshDataArrays[i] ,
					mesh ,
					MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontRecalculateBounds
				);
				mesh.UploadMeshData( false );
			}
			Profiler.EndSample();

			_initializationSystem.numBatchesToPush = 0;
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
