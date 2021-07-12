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
		Segments.SegmentRenderingSystem _segmentsSystem;
		Segments.Batch _batch;

		
		void OnEnable ()
		{
			_meshRenderer = GetComponent<MeshRenderer>();

			// create segment buffer:
			_segmentsSystem = Segments.Core.GetRenderingSystem();
			_segmentsSystem.CreateBatch( out _batch , _materialOverride );
			
			// initialize buffer size:
			_batch.buffer.Length = 12;
		}


		void OnDisable ()
		{
			if( _batch!=null )
			{
				_batch.Dependency.Complete();
				_batch.Dispose();
			}
		}


		void Update ()
		{
			_batch.Dependency.Complete();
			
			var bounds = _meshRenderer.bounds;
			int index = 0;
			var job = new Segments.Plot.BoxJob(
				segments:	_batch.buffer ,
				index:		ref index ,
				size:		bounds.size ,
				pos:		bounds.center ,
				rot:		quaternion.identity
			);

			_batch.Dependency = job.Schedule( _batch.Dependency );
		}


	}
}
