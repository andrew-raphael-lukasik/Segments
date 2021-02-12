using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

namespace Segments
{
	[WorldSystemFilter( WorldSystemFilterFlags.Default )]
	[UpdateInGroup( typeof(InitializationSystemGroup) )]
	public class SegmentTransformSystem : SystemBase
	{
		protected override void OnUpdate ()
		{
			Camera camera = Camera.main;
			if( camera==null ) camera = Camera.current;
			if( camera==null ) return;
			Transform cameraTransform = camera.transform;
			
			if( !camera.orthographic )// perspective-projection camera code path
			{
				float3 cameraPosition = cameraTransform.position;
				Entities
					.WithName("LTW_update_job_perspective_projection")
					.ForEach( ( ref LocalToWorld ltw , in Segment segment , in SegmentWidth segmentWidth ) =>
					{
						float3 lineVec = segment.end - segment.start;
						var rot = quaternion.LookRotation( math.normalize(lineVec) , math.normalize(cameraPosition-segment.start) );
						var pos = segment.start;
						var scale = new float3{ x=segmentWidth.Value , y=1f , z=math.length(lineVec) };
						ltw.Value = float4x4.TRS( pos , rot , scale );
					})
					.WithBurst().ScheduleParallel();
			}
			else// orthographic-projection camera
			{
				quaternion cameraRotation = cameraTransform.rotation;
				Entities
					.WithName("LTW_update_job_orthographic_projection")
					.ForEach( ( ref LocalToWorld ltw , in Segment segment , in SegmentWidth segmentWidth ) =>
					{
						float3 lineVec = segment.end - segment.start;
						var rot = quaternion.LookRotation( math.normalize(lineVec) , math.mul(cameraRotation,new float3{z=-1}) );
						var pos = segment.start;
						var scale = new float3{ x=segmentWidth.Value , y=1f , z=math.length(lineVec) };
						ltw.Value = float4x4.TRS( pos , rot , scale );
					})
					.WithBurst().ScheduleParallel();
			}

			Entities
				.WithChangeFilter<Segment,SegmentWidth>()
				.WithName("aspect_ratio_update_job")
				.ForEach( ( ref SegmentAspectRatio aspectRatio , in Segment segment , in SegmentWidth segmentWidth ) =>
				{
					aspectRatio.Value = (float)segmentWidth.Value / math.length( segment.end - segment.start );
				})
				.WithBurst().ScheduleParallel();
		}
	}
}
