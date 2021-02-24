using UnityEngine;

using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Burst;

using Random = Unity.Mathematics.Random;

namespace Segments.Samples
{
	[ExecuteAlways]
	[AddComponentMenu("")]
	class StressTests : MonoBehaviour
	{
		
		[SerializeField] Material _materialOverride = null;
		[SerializeField] float _widthOverride = 0.003f;
		[SerializeField][Range(2,Segments.NativeListToSegmentsSystem.k_max_num_segments)] int _numSegments;
		
		NativeList<float3x2> _segments;
		Segments.NativeListToSegmentsSystem _segmentsSystem;
		public JobHandle Dependency;


		void OnEnable ()
		{
			_segmentsSystem = Segments.Core.GetWorld().GetExistingSystem<Segments.NativeListToSegmentsSystem>();

			// initialize segment list:
			Entity prefab = Segments.Core.GetSegmentPrefabCopy( _materialOverride , _widthOverride );
			_segmentsSystem.CreateBatch( prefab , out _segments );
		}


		void OnDisable ()
		{
			Dependency.Complete();
			_segmentsSystem.DestroyBatch( ref _segments , true );
		}


		void Update ()
		{
			_segments.Length = _numSegments;
			var job = new MyJob{
				transform		= transform.localToWorldMatrix ,
				numSegments		= _numSegments ,
				segments		= _segments.AsArray().Slice()
			};
			
			Dependency = job.Schedule( arrayLength:_segments.Length , innerloopBatchCount:128 , dependsOn:Dependency );
			_segmentsSystem.Dependencies.Add( Dependency );
		}


		[BurstCompile]
		public struct MyJob : IJobParallelFor
		{
			public float4x4 transform;
			public int numSegments;
			[WriteOnly] public NativeSlice<float3x2> segments;
			void IJobParallelFor.Execute ( int index )
			{
				float t0 = (float)index / (float)numSegments;
				float t1 = (float)(index+1) / (float)numSegments;
				
				float rnd0 = Random.CreateFromIndex( (uint) index ).NextFloat();
				float rnd1 = Random.CreateFromIndex( (uint) index + 1 ).NextFloat();
				float3 v0 =  math.transform( transform , new float3{ x=t0 , y=rnd0 } );
				float3 v1 =  math.transform( transform , new float3{ x=t1 , y=rnd1 } );
				
				segments[index] = new float3x2{ c0=v0 , c1=v1 };
			}
		}
		
	}
}
