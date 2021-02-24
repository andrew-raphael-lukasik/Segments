using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(UpdatePresentationSystemGroup) )]
	public class SegmentTransformSystem : SystemBase
	{
		protected override void OnUpdate ()
		{
			Camera camera = Camera.main;
			#if UNITY_EDITOR
			{
				var sceneView = UnityEditor.SceneView.lastActiveSceneView;
				if( sceneView!=null && sceneView.hasFocus )
					camera = sceneView.camera;
			}
			#endif
			if( camera==null ) return;// no camera found
			Transform cameraTransform = camera.transform;
			
			if( !camera.orthographic )// perspective-projection camera code path
			{
				float3 cameraPosition = cameraTransform.position;
				Entities
					.WithName("LTW_update_for_perspective_projection_job")
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
					.WithName("LTW_update_for_orthographic_projection_job")
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
				.WithName("aspect_ratio_update_job")
				.WithChangeFilter<Segment,SegmentWidth>()
				.ForEach( ( ref SegmentAspectRatio aspectRatio , in Segment segment , in SegmentWidth segmentWidth ) =>
				{
					aspectRatio.Value = (float)segmentWidth.Value / math.length( segment.end - segment.start );
				})
				.WithBurst().ScheduleParallel();
		}
	}
}
