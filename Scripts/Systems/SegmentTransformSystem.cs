using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

namespace EcsLineRenderer
{
	[WorldSystemFilter( WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor )]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	public class SegmentTransformSystem : SystemBase
	{
		protected override void OnUpdate ()
		{
			#if UNITY_EDITOR
			bool debug = Input.GetKey( KeyCode.LeftAlt );
			#endif

			Camera camera = Camera.main;
			if( camera==null ) camera = Camera.current;
			if( camera==null ) return;
			Transform cameraTransform = camera.transform;
			
			if( !camera.orthographic )// perspective-projection camera code path
			{
				float3 cameraPosition = cameraTransform.position;
				Entities
					.WithName("LTR_update_job_perspective_projection")
					.ForEach( ( ref LocalToWorld ltr , in Segment segment , in SegmentWidth segmentWidth ) =>
					{
						float3 lineVec = segment.end - segment.start;
						var rot = quaternion.LookRotation( math.normalize(lineVec) , math.normalize(cameraPosition-segment.start) );
						var pos = segment.start;
						var scale = new float3{ x=segmentWidth.Value , y=1f , z=math.length(lineVec) };
						ltr.Value = float4x4.TRS( pos , rot , scale );

						#if UNITY_EDITOR
						if( debug )
						{
							Color white = new Color{ r=1 , g=1 , b=1 , a=0.1f };
							float3 c = pos + lineVec*0.5f;
							Debug.DrawLine( c , c + math.mul( rot , new float3{x=1} ) , Color.red , 0.01f );
							Debug.DrawLine( c , c + math.mul( rot , new float3{y=1} ) , Color.green , 0.01f );
							Debug.DrawLine( c , c + math.mul( rot , new float3{z=1} ) , Color.blue , 0.01f );
						}
						#endif
					}).ScheduleParallel();
				}
			else// orthographic-projection camera
			{
				quaternion cameraRotation = cameraTransform.rotation;
				Entities
					.WithName("LTR_update_job_orthographic_projection")
					.ForEach( ( ref LocalToWorld ltr , in Segment segment , in SegmentWidth segmentWidth ) =>
					{
						float3 lineVec = segment.end - segment.start;
						var rot = quaternion.LookRotation( math.normalize(lineVec) , math.mul(cameraRotation,new float3{z=-1}) );
						var pos = segment.start;
						var scale = new float3{ x=segmentWidth.Value , y=1f , z=math.length(lineVec) };
						ltr.Value = float4x4.TRS( pos , rot , scale );

						#if UNITY_EDITOR
						if( debug )
						{
							Color white = new Color{ r=1 , g=1 , b=1 , a=0.1f };
							float3 c = pos + lineVec*0.5f;
							Debug.DrawLine( c , c + math.mul( rot , new float3{x=1} ) , Color.red , 0.01f );
							Debug.DrawLine( c , c + math.mul( rot , new float3{y=1} ) , Color.green , 0.01f );
							Debug.DrawLine( c , c + math.mul( rot , new float3{z=1} ) , Color.blue , 0.01f );
						}
						#endif
					}).ScheduleParallel();
			}

			Entities
				.WithName("aspect_ratio_update_job")
				.ForEach( ( ref SegmentAspectRatio aspectRatio , in Segment segment , in SegmentWidth segmentWidth ) =>
				{
					aspectRatio.Value = (float)segmentWidth.Value / math.length( segment.end - segment.start );
				}).ScheduleParallel();
		}
	}
}
