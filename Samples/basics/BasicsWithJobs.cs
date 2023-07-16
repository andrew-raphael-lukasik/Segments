using UnityEngine;
using Unity.Mathematics;
using Unity.Jobs;
using Unity.Collections;
using Unity.Entities;

namespace Samples
{
	/// <summary>
	/// This is how you make line-plotting faster with Burst and Jobs.
	/// </summary>
	[AddComponentMenu("")]
	[ExecuteAlways]
	public class BasicsWithJobs : MonoBehaviour
	{

		Entity _segments;
		EntityManager _entityManager;

		void OnEnable () => Segments.Core.CreateBatch( out _segments , out _entityManager );
		void OnDisable () => Segments.Core.DestroyBatch( _segments );

		void Update ()
		{
			Segments.Core.CompleteDependency();

			var segments = Segments.Utilities.GetSegmentBuffer( _segments , _entityManager );
			segments.Length = 3;

			var jobHandle = new MyBasicJob
			{
				Buffer			= segments.AsNativeArray() ,
				LocalToWorld	= transform.localToWorldMatrix
			}.Schedule(  );
			
			Segments.Core.AddDependency( jobHandle );
		}

		[Unity.Burst.BurstCompile]
		struct MyBasicJob : IJob
		{
			public NativeArray<float3x2> Buffer;
			public float4x4 LocalToWorld;
			void IJob.Execute ()
			{
				float4 c0 = LocalToWorld.c0;
				float4 c1 = LocalToWorld.c1;
				float4 c2 = LocalToWorld.c2;
				float4 c3 = LocalToWorld.c3;

				float3 position	= new float3( c3.x , c3.y , c3.z );
				float3 right	= new float3( c0.x , c0.y , c0.z );
				float3 up		= new float3( c1.x , c1.y , c1.z );
				float3 forward	= new float3( c2.x , c2.y , c2.z );

				Buffer[0] = new float3x2( position , position+right );
				Buffer[1] = new float3x2( position , position+up );
				Buffer[2] = new float3x2( position , position+forward );
			}
		}

	}
}
