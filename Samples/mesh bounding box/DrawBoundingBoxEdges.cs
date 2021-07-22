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

		MeshRenderer _meshRenderer = null;
		Segments.Batch _segments;

		
		void OnEnable ()
		{
			_meshRenderer = GetComponent<MeshRenderer>();

			// create segment buffer:
			Segments.Core.CreateBatch( out _segments , _materialOverride );
			
			// initialize buffer size:
			_segments.buffer.Length = 12;
		}


		void OnDisable ()
		{
			if( _segments!=null )
			{
				_segments.Dependency.Complete();
				_segments.Dispose();
			}
		}


		void Update ()
		{
			_segments.Dependency.Complete();
			
			var bounds = _meshRenderer.bounds;
			int index = 0;
			var job = new Segments.Plot.BoxJob(
				segments:	_segments.buffer ,
				index:		ref index ,
				size:		bounds.size ,
				pos:		bounds.center ,
				rot:		quaternion.identity
			);

			_segments.Dependency = job.Schedule( _segments.Dependency );
		}


	}
}
