using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;

namespace Samples
{
	/// <summary>
	/// Bare-minimum of code that will result in lines being drawn on screen.
	/// </summary>
	[AddComponentMenu("")]
	[ExecuteAlways]
	public class Basics : MonoBehaviour
	{
		
		Entity _segments;
		
		void OnEnable () => Segments.Core.CreateBatch( out _segments );
		void OnDisable () => Segments.Core.DestroyBatch( _segments );
		
		void Update ()
		{
			Segments.Core.CompleteDependency();

			var segments = Segments.Core.GetSegmentBuffer( _segments );
			segments.Length = 3;
			Vector3 position = transform.position;
			segments[0] = new float3x2( position , position+transform.right );
			segments[1] = new float3x2( position , position+transform.up );
			segments[2] = new float3x2( position , position+transform.forward );
		}

	}
}
