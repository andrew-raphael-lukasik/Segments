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
		EntityManager _entityManager;
		
		void OnEnable () => Segments.Core.CreateBatch( out _segments , out _entityManager );
		void OnDisable () => Segments.Core.DestroyBatch( _segments );
		
		void Update ()
		{
			Segments.Core.CompleteDependency();

			Vector3 position = transform.position;
			var segments = Segments.Utilities.GetSegmentBuffer( _segments , _entityManager );
			segments.Length = 3;
			segments[0] = new float3x2( position , position+transform.right );
			segments[1] = new float3x2( position , position+transform.up );
			segments[2] = new float3x2( position , position+transform.forward );
		}

	}
}
