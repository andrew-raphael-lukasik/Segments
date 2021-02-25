using Unity.Mathematics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Transforms;
using Unity.Rendering;
using Unity.Burst;
using UnityEngine;

namespace Segments
{
	[WorldSystemFilter( 0 )]
	[UpdateInGroup( typeof(UpdatePresentationSystemGroup) )]
	public class SegmentTransformSystem : SystemBase
	{
		EntityQuery _query;
		protected override void OnCreate ()
		{
			_query = EntityManager.CreateEntityQuery(
					ComponentType.ReadWrite<LocalToWorld>()
				,	ComponentType.ReadOnly<Segment>()
				,	ComponentType.ReadOnly<SegmentWidth>()
			);
			_query.AddChangedVersionFilter( typeof(Segment) );
			_query.AddChangedVersionFilter( typeof(SegmentWidth) );
		}
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
			var localToWorldHandle		= EntityManager.GetComponentTypeHandle<LocalToWorld>( isReadOnly:false );
			var segmentHandle			= EntityManager.GetComponentTypeHandle<Segment>( isReadOnly:true );
			var segmentWidthHandle		= EntityManager.GetComponentTypeHandle<SegmentWidth>( isReadOnly:true );
			
			Dependency = JobHandle.CombineDependencies( Dependency , _query.GetDependency() );
			if( !camera.orthographic )// perspective-projection camera code path
			{
				float3 cameraPosition = cameraTransform.position;
				var job = new SegmentTransformPerspectiveProjectionJob{
					cameraPosition			= cameraPosition ,
					localToWorldHandle		= localToWorldHandle ,
					segmentHandle			= segmentHandle ,
					segmentWidthHandle		= segmentWidthHandle
				};
				Dependency = job.ScheduleParallel( _query , batchesPerChunk:4 , Dependency );
			}
			else// orthographic-projection camera
			{
				var job = new SegmentTransformOrthographicProjectionJob{
					cameraRotation			= cameraTransform.rotation ,
					localToWorldHandle		= localToWorldHandle ,
					segmentHandle			= segmentHandle ,
					segmentWidthHandle		= segmentWidthHandle
				};
				Dependency = job.ScheduleParallel( _query , batchesPerChunk:4 , Dependency );
			}
			_query.AddDependency( Dependency );
		}
	}

	[BurstCompile]
	public struct SegmentTransformPerspectiveProjectionJob : IJobEntityBatch
	{
		public float3 cameraPosition;
		public ComponentTypeHandle<LocalToWorld> localToWorldHandle;
		[ReadOnly] public  ComponentTypeHandle<Segment> segmentHandle;
		[ReadOnly] public ComponentTypeHandle<SegmentWidth> segmentWidthHandle;
		void IJobEntityBatch.Execute ( ArchetypeChunk chunk , int batchIndex )
		{
			var ltw = chunk.GetNativeArray( localToWorldHandle );
			var segment = chunk.GetNativeArray( segmentHandle );
			var segmentWidth = chunk.GetNativeArray( segmentWidthHandle );
			for( int i=0 ; i<ltw.Length ; i++ )
			{
				float3 p0 = segment[i].start;
				float3 p1 = segment[i].end;
				float3 lineVec = p1 - p0;
				var rot = quaternion.LookRotation( math.normalize(lineVec) , math.normalize(cameraPosition-p0) );
				var scale = new float3{ x=segmentWidth[i].Value , y=1f , z=math.length(lineVec) };
				ltw[i] = new LocalToWorld{
					Value = float4x4.TRS( p0 , rot , scale )
				};
			}
		}
	}

	[BurstCompile]
	public struct SegmentTransformOrthographicProjectionJob : IJobEntityBatch
	{
		public quaternion cameraRotation;
		public ComponentTypeHandle<LocalToWorld> localToWorldHandle;
		[ReadOnly] public  ComponentTypeHandle<Segment> segmentHandle;
		[ReadOnly] public ComponentTypeHandle<SegmentWidth> segmentWidthHandle;
		void IJobEntityBatch.Execute ( ArchetypeChunk chunk , int batchIndex )
		{
			var ltw = chunk.GetNativeArray( localToWorldHandle );
			var segment = chunk.GetNativeArray( segmentHandle );
			var segmentWidth = chunk.GetNativeArray( segmentWidthHandle );
			for( int i=0 ; i<ltw.Length ; i++ )
			{
				float3 p0 = segment[i].start;
				float3 p1 = segment[i].end;
				float3 lineVec = p1 - p0;
				var rot = quaternion.LookRotation( math.normalize(lineVec) , math.mul(cameraRotation,new float3{z=-1}) );
				var scale = new float3{ x=segmentWidth[i].Value , y=1f , z=math.length(lineVec) };
				ltw[i] = new LocalToWorld{
					Value = float4x4.TRS( p0 , rot , scale )
				};
			}
		}
	}

	[BurstCompile]
	[UpdateInGroup( typeof(UpdatePresentationSystemGroup) )]
	public struct SegmentAspectRatioSystem : ISystemBase
	{
		EntityQuery _query;
		void ISystemBase.OnCreate ( ref SystemState state )
		{
			_query = state.GetEntityQuery(
					ComponentType.ReadWrite<SegmentAspectRatio>()
				,	ComponentType.ReadOnly<Segment>()
				,	ComponentType.ReadOnly<SegmentWidth>()
			);
			_query.AddChangedVersionFilter( typeof(Segment) );
			_query.AddChangedVersionFilter( typeof(SegmentWidth) );
		}
		void ISystemBase.OnUpdate ( ref SystemState state )
		{
			var cmd = state.EntityManager;
			var job = new SegmentAspectRatioJob{
				segmentAspectRatioHandle	= cmd.GetComponentTypeHandle<SegmentAspectRatio>( isReadOnly:false ) ,
				segmentHandle				= cmd.GetComponentTypeHandle<Segment>( isReadOnly:true ) ,
				segmentWidthHandle			= cmd.GetComponentTypeHandle<SegmentWidth>( isReadOnly:true )
			};
			state.Dependency = job.ScheduleParallel( _query , batchesPerChunk:4 , state.Dependency );
		}
		void ISystemBase.OnDestroy ( ref SystemState state ) {}
	}

	[BurstCompile]
	public struct SegmentAspectRatioJob : IJobEntityBatch
	{
		public ComponentTypeHandle<SegmentAspectRatio> segmentAspectRatioHandle;
		[ReadOnly] public  ComponentTypeHandle<Segment> segmentHandle;
		[ReadOnly] public ComponentTypeHandle<SegmentWidth> segmentWidthHandle;
		void IJobEntityBatch.Execute ( ArchetypeChunk chunk , int batchIndex )
		{
			var aspectRatio = chunk.GetNativeArray( segmentAspectRatioHandle );
			var segment = chunk.GetNativeArray( segmentHandle );
			var segmentWidth = chunk.GetNativeArray( segmentWidthHandle );
			for( int i=0 ; i<aspectRatio.Length ; i++ )
			{
				aspectRatio[i] = new SegmentAspectRatio{
					Value = (float)segmentWidth[i].Value / math.length( segment[i].end - segment[i].start )
				};
			}
		}
	}

}
