using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;

namespace Samples
{
	[ExecuteAlways]
	[AddComponentMenu("")]
	[RequireComponent( typeof(MeshRenderer) )]
	class DrawBoundingBoxEdges : MonoBehaviour
	{

		[SerializeField] Material _materialOverride = null;

		MeshRenderer _meshRenderer = null;
		Entity _segments;
		
		void OnEnable ()
		{
			_meshRenderer = GetComponent<MeshRenderer>();

			// create segment buffer:
			Segments.Core.CreateBatch( out _segments , _materialOverride );
			
			// set buffer size as it won't change here:
			var buffer = Segments.Core.GetSegmentBuffer( _segments );
			buffer.Length = 12;
		}

		void OnDisable () => Segments.Core.DestroyBatch( _segments );

		void Update ()
		{
			Segments.Core.CompleteDependency();

			var buffer = Segments.Core.GetSegmentBuffer( _segments );
			var bounds = _meshRenderer.bounds;
			int index = 0;
			var jobHandle = new Segments.Plot.BoxJob(
				segments:	buffer ,
				index:		ref index ,
				size:		bounds.size ,
				pos:		bounds.center ,
				rot:		quaternion.identity
			).Schedule();

			Segments.Core.AddDependency( jobHandle );
		}

	}
}
