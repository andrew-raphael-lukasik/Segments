using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using Debug = UnityEngine.Debug;

using Unity.Mathematics;
using Unity.Entities;
using Unity.Rendering;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(UpdatePresentationSystemGroup) )]
	internal class SegmentRenderingSystem : SystemBase
	{


		protected override void OnCreate ()
		{
			RenderPipelineManager.beginCameraRendering += OnBeginCameraRendering;
		}


		protected override void OnDestroy ()
		{
			RenderPipelineManager.beginCameraRendering -= OnBeginCameraRendering;
		}


		protected override void OnUpdate ()
		{
			
		}


		void OnBeginCameraRendering ( ScriptableRenderContext context , Camera camera )
		{
			#if UNITY_EDITOR
			if( camera.name=="Preview Scene Camera" ) return;
			#endif

			var propertyBlock = new MaterialPropertyBlock{};
			var batches = Core.Batches;
			for( int i=batches.Count-1 ; i!=-1 ; i-- )
			{
				var batch = batches[i];
				Graphics.DrawMesh( batch.mesh , Vector3.zero , quaternion.identity , batch.material , 0 , camera , 0 , propertyBlock , false , true , true );
			}
		}


	}
}
