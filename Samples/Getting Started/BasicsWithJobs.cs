using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;

namespace Segments.Samples
{
	/// <summary>
	/// This is how you make line-plotting faster with Burst and Jobs.
	/// </summary>
	[AddComponentMenu("")]
	[ExecuteAlways]
	public class BasicsWithJobs : MonoBehaviour
	{

		Segments.Batch _batch;

		void OnEnable () => Segments.Core.CreateBatch( out _batch );
		void OnDisable () => _batch.Dispose();

		void Update ()
		{
			_batch.Dependency.Complete();

			var buffer = _batch.buffer;
			buffer.Length = 3;
			var job = new MyJob{
				Buffer	= buffer.AsArray() ,
				LocalToWorld	= transform.localToWorldMatrix
			};
			
			_batch.Dependency = job.Schedule( _batch.Dependency );
		}

		[BurstCompile]
		struct MyJob : IJob
		{
			public NativeArray<float3x2> Buffer;
			public float4x4 LocalToWorld;
			void IJob.Execute ()
			{
				float3 position = new float3{ x=LocalToWorld.c3.x , y=LocalToWorld.c3.y , z=LocalToWorld.c3.z }; 
				float3x3 rotation = new float3x3(LocalToWorld);
				float3 right = math.mul( rotation , new float3{ x=1 } );
				float3 up = math.mul( rotation , new float3{ y=1 } );
				float3 forward = math.mul( rotation , new float3{ z=1 } );
				Buffer[0] = new float3x2( position , position+right );
				Buffer[1] = new float3x2( position , position+up );
				Buffer[2] = new float3x2( position , position+forward );
			}
		}

	}
}
