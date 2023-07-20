using UnityEngine;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

namespace Samples
{
	[ExecuteAlways]
	[AddComponentMenu("")]
	class StressTests : MonoBehaviour
	{

		[SerializeField] Material _srcMaterial = null;
		[SerializeField] int _numSegments = 128;
		[SerializeField] float _frequency = 16;
		[SerializeField] bool _everyFrame = false;

		Entity _segments;
		EntityManager _entityManager;

		void OnEnable ()
		{
			Segments.Core.CreateBatch( out _segments , out _entityManager , _srcMaterial );
		}

		void OnDisable () => Segments.Core.DestroyBatch( _segments );

		void Update ()
		{
			Segments.Core.Query.CompleteDependency();

			var segments = Segments.Utilities.GetSegmentBuffer( _segments , _entityManager );
			if( segments.Length!=_numSegments || _everyFrame )
			{
				segments.Length = _numSegments;

				// schedule new job:
				var jobHandle = new StressTestJob
				{
					Transform       = transform.localToWorldMatrix ,
					NumSegments     = _numSegments ,
					Segments        = segments.AsNativeArray() ,
					Offset          = Time.time ,
					Frequency       = _frequency ,
				}.Schedule( arrayLength: _numSegments , indicesPerJobCount: 64 );

				Segments.Core.AddDependency( jobHandle );
			}
		}

		[Unity.Burst.BurstCompile]
		public struct StressTestJob : IJobParallelForBatch
		{
			public float4x4 Transform;
			public int NumSegments;
			public float Offset;
			public float Frequency;
			[WriteOnly] public NativeArray<float3x2> Segments;
			void IJobParallelForBatch.Execute ( int startIndex , int count )
			{
				float3 translation = new float3(Transform.c3.x , Transform.c3.y , Transform.c3.z);
				float3 right = Transform.Right();
				float3 up = Transform.Up();

				for( int i = 0 ; i<count ; i++ )
				{
					int index = startIndex + i;

					float t0 = (float)index / (float)NumSegments;
					float t1 = (float)(index+1) / (float)NumSegments;
					float2 amp = math.sin(Frequency * new float2
					{
						x = t0*math.PI*2f + Offset ,
						y = t1*math.PI*2f + Offset
					});
					float3 vec0 = translation + right*t0 + up*amp.x;
					float3 vec1 = translation + right*t1 + up*amp.y;
					Segments[index] = new float3x2 { c0=vec0 , c1=vec1 };
				}
			}
		}

	}
}
