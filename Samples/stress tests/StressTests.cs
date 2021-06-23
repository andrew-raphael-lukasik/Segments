using UnityEngine;
using UnityEngine.Assertions;

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
		
		[SerializeField] Material _srcMaterial = null;
		[SerializeField][Min(1)] int _numSegments;
		
		Segments.SegmentRenderingSystem _segmentsSystem;
		
		[Header("Read Only:")]
		[SerializeField] Segments.Batch _batch;

		public JobHandle Dependency;


		void OnEnable ()
		{
			_segmentsSystem = Segments.Core.GetWorld().GetExistingSystem<Segments.SegmentRenderingSystem>();
			_segmentsSystem.CreateBatch( out _batch , _srcMaterial );
		}


		void OnDisable ()
		{
			Dependency.Complete();
			_batch.Dispose();
		}


		void Update ()
		{
			Dependency.Complete();
			
			if( _batch.Length!=_numSegments )
			{
				_batch.Length = _numSegments;
				var job = new MyJob{
					transform		= transform.localToWorldMatrix ,
					numSegments		= _numSegments ,
					segments		= _batch.Segments.AsArray().Slice()
				};
				
				Dependency = job.Schedule( arrayLength:_batch.Length , innerloopBatchCount:128 , dependsOn:Dependency );
				_batch.Dependency = Dependency;
				// _batch.isBufferDirty = true;

				Dependency.Complete();
				int i = 0;
				_batch.Segments[i++] = new float3x2{
					c0 = new float3{ x=0 , y=0 , z=-4+i } ,
					c1 = new float3{ x=0 , y=0.1f , z=-4+i }
				};

				_batch.Segments[i++] = new float3x2{
					c0 = new float3{ x=0 , y=0 , z=-4+i } ,
					c1 = new float3{ x=0 , y=1f , z=-4+i }
				};
			}
			// else _batch.isBufferDirty = false;
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
