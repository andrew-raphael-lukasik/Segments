using System.Runtime.CompilerServices;
using UnityEngine;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Jobs;

namespace Segments
{
	public static class Core
	{

		static World world;
		static EntityManager entityManager;
		static EntityArchetype segmentArchetype = default(EntityArchetype);
		static Entity defaultPrefab;


		public static World GetWorld ()
		{
			if( world!=null )
				return world;
			else
			{
				world = World.DefaultGameObjectInjectionWorld;
				DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups( world , Prototypes.worldSystems );
				entityManager = world.EntityManager;
				
				defaultPrefab = entityManager.CreateEntity( GetSegmentArchetype() );
				entityManager.SetComponentData<Segment>( defaultPrefab , Prototypes.segment );
				entityManager.SetComponentData<SegmentWidth>( defaultPrefab , Prototypes.segmentWidth );
				entityManager.SetComponentData<SegmentAspectRatio>( defaultPrefab , new SegmentAspectRatio{ Value = 1f } );
				entityManager.AddComponentData<RenderBounds>( defaultPrefab , Prototypes.renderBounds );
				entityManager.AddComponentData<LocalToWorld>( defaultPrefab , new LocalToWorld { Value = float4x4.TRS( new float3{} , quaternion.identity , new float3{x=1,y=1,z=1} ) });
				
				var renderMesh = Prototypes.renderMesh;
				entityManager.SetSharedComponentData<RenderMesh>( defaultPrefab , renderMesh );
				// commander.SetComponentData<MaterialColor>( _defaultPrefab , new MaterialColor{ Value=new float4{x=1,y=1,z=1,w=1} } );// change: initialize manually
				
				#if ENABLE_HYBRID_RENDERER_V2
				entityManager.SetComponentData( defaultPrefab , new BuiltinMaterialPropertyUnity_RenderingLayer{ Value = new uint4{ x=(uint)renderMesh.layer } } );
				entityManager.SetComponentData( defaultPrefab , new BuiltinMaterialPropertyUnity_LightData{ Value = new float4{ z=1 } } );
				#endif

				return world;
			}
		}
		

		public static Entity GetSegmentPrefabCopy ()
		{
			Initialize();
			Entity copy = entityManager.Instantiate( defaultPrefab );
			entityManager.AddComponent<Prefab>( copy );
			return copy;
		}
		public static Entity GetSegmentPrefabCopy ( Material material )
		{
			Entity copy = GetSegmentPrefabCopy();
			var renderMesh = entityManager.GetSharedComponentData<RenderMesh>( copy );
			renderMesh.material = material;
			entityManager.SetSharedComponentData<RenderMesh>( copy , renderMesh );
			return copy;
		}
		public static Entity GetSegmentPrefabCopy ( float width )
		{
			Entity copy = GetSegmentPrefabCopy();
			entityManager.SetComponentData( copy , new SegmentWidth{ Value=(half)width } );
			return copy;
		}
		public static Entity GetSegmentPrefabCopy ( Material material , float width )
		{
			Entity copy = GetSegmentPrefabCopy( material );
			entityManager.SetComponentData( copy , new SegmentWidth{ Value=(half)width } );
			return copy;
		}


		public static EntityArchetype GetSegmentArchetype ()
		{
			if( segmentArchetype.Valid )
				return segmentArchetype;
			
			segmentArchetype = entityManager.CreateArchetype( Prototypes.segment_prefab_components );
			return segmentArchetype;
		}


		public static void DestroyAllSegments ()
		{
			var query = entityManager.CreateEntityQuery( new ComponentType[]{ typeof(Segment) } );
			entityManager.DestroyEntity( query );
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Initialize () => GetWorld();

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _WorldInitializedWarningCheck ()
		{
			if( world==null || !world.IsCreated )
				Debug.LogError($"Call `{nameof(Segments)}.{nameof(Core)}.{nameof(Initialize)}()` first.");
		}


	}
}
