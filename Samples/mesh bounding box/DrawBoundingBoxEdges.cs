using UnityEngine;

using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Segments.Samples
{
	[ExecuteAlways]
	[AddComponentMenu("")]
	[RequireComponent( typeof(MeshRenderer) )]
	class DrawBoundingBoxEdges : MonoBehaviour
	{

		[SerializeField] Material _materialOverride = null;
		[SerializeField] float _widthOverride = 0.003f;

		MeshRenderer _meshRenderer = null;
		
		NativeArray<float3x2> _segments;
		Segments.NativeArrayToSegmentsSystem _segmentsSystem;
		public JobHandle Dependency;

		
		void OnEnable ()
		{
			_meshRenderer = GetComponent<MeshRenderer>();

			var world = Segments.Core.GetWorld();
			_segmentsSystem = world.GetExistingSystem<Segments.NativeArrayToSegmentsSystem>();

			// create segment buffer:
			Entity prefab = Segments.Core.GetSegmentPrefabCopy( _materialOverride , _widthOverride );
			_segmentsSystem.CreateBatch(
				segmentPrefab:	prefab ,
				length:			12 ,// box is 12 segments
				buffer:			out _segments
			);
		}


		void OnDisable ()
		{
			Dependency.Complete();
			_segmentsSystem.DestroyBatch( ref _segments );
		}


		void Update ()
		{
			var bounds = _meshRenderer.bounds;
			int index = 0;
			var job = new Segments.Plot.BoxJob(
				segments:	_segments ,
				index:		ref index ,
				size:		bounds.size ,
				pos:		bounds.center ,
				rot:		quaternion.identity
			);

			Dependency = job.Schedule( Dependency );
			_segmentsSystem.Dependencies.Add( Dependency );
		}


	}
}
