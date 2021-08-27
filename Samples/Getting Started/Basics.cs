using UnityEngine;
using Unity.Mathematics;

namespace Segments.Samples
{
	/// <summary>
	/// Bare-minimum of code that will result in lines being drawn on screen.
	/// </summary>
	[AddComponentMenu("")]
	[ExecuteAlways]
	public class Basics : MonoBehaviour
	{
		Segments.Batch _batch;
		void OnEnable () => Segments.Core.CreateBatch( out _batch );
		void OnDisable () => _batch.Dispose();
		void Update ()
		{
			_batch.Dependency.Complete();

			var buffer = _batch.buffer;
			buffer.Length = 3;
			Vector3 position = transform.position;
			buffer[0] = new float3x2( position , position+transform.right );
			buffer[1] = new float3x2( position , position+transform.up );
			buffer[2] = new float3x2( position , position+transform.forward );
		}
	}
}
