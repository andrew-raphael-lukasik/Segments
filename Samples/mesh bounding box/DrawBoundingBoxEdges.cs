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
		EntityManager _entityManager;
		
		void OnEnable ()
		{
			_meshRenderer = GetComponent<MeshRenderer>();

			// create segment buffer:
			Segments.Core.CreateBatch( out _segments , out _entityManager , _materialOverride );
			
			// set buffer size as it won't change here:
			var buffer = Segments.Utilities.GetSegmentBuffer( _segments , _entityManager );
			buffer.Length = 12;
		}

		void OnDisable () => Segments.Core.DestroyBatch( _segments );

		void Update ()
		{
			Segments.Core.CompleteDependency();

			var buffer = Segments.Utilities.GetSegmentBuffer( _segments , _entityManager );
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
