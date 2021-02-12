using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Assertions;

using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel;
using Unity.Collections.LowLevel.Unsafe;

namespace EcsLineRenderer
{
	public static class LineRendererWorld
	{


		static World _world = null;
		static readonly string k_world_name = nameof(EcsLineRenderer);

		public static bool IsCreated => _world!=null && _world.IsCreated;

		static Entity _segmentPrefab = Entity.Null;
		static EntityArchetype _segmentArchetype = default(EntityArchetype);


		public static World GetOrCreateWorld ()
		{
			if( IsCreated ) return _world;
			return _world = CreateNewDedicatedRenderingWorld( k_world_name );
		}
		
		public static World GetWorld ()
		{
			_WorldExistsWarningCheck();
			return _world;
		}
		
		public static EntityManager GetEntityManager ()
		{
			_WorldExistsWarningCheck();
			return _world.EntityManager;
		}

		public static void Dispose () => _world?.Dispose();


		public static Entity GetSegmentPrefabCopy ()
		{
			var command = GetEntityManager();
			if( !command.Exists(_segmentPrefab) )
			{
				_segmentPrefab = command.CreateEntity( GetSegmentArchetype() );
				command.SetComponentData<Segment>( _segmentPrefab , Prototypes.segment );
				command.SetComponentData<SegmentWidth>( _segmentPrefab , Prototypes.segmentWidth );
				command.SetComponentData<SegmentAspectRatio>( _segmentPrefab , new SegmentAspectRatio{
					Value = 1f
				} );
				command.AddComponentData<RenderBounds>( _segmentPrefab , Prototypes.renderBounds );
				command.AddComponentData<LocalToWorld>( _segmentPrefab , new LocalToWorld {
					Value = float4x4.TRS( new float3{} , quaternion.identity , new float3{x=1,y=1,z=1} )
				});
				command.SetSharedComponentData<RenderMesh>( _segmentPrefab , Prototypes.renderMesh );

				#if UNITY_EDITOR
				command.SetName( _segmentPrefab , "segment prefab" );
				#endif
			}
			
			var copy = command.Instantiate( _segmentPrefab );
			command.AddComponent<Prefab>( copy );
			#if UNITY_EDITOR
			command.SetName( copy , "segment" );
			#endif
			return copy;
		}

		public static EntityArchetype GetSegmentArchetype ()
		{
			var command = GetEntityManager();
			if( _segmentArchetype.Valid )
				return _segmentArchetype;
			
			_segmentArchetype = command.CreateArchetype( Prototypes.segment_prefab_components );
			return _segmentArchetype;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void _WorldExistsWarningCheck ()
		{
			if( _world==null || !_world.IsCreated )
				Debug.LogWarning($"{k_world_name} does not exists yet. Call `{nameof(GetOrCreateWorld)}()` first.");
		}


		public static World CreateNewDedicatedRenderingWorld ( string name )
		{
			var world = new World( name );
			{
				var systems = new System.Type[] {
					#if ENABLE_HYBRID_RENDERER_V2
						typeof(HybridRendererSystem)
					#else
						typeof(RenderMeshSystemV2)
					#endif

						// fixes: warning from RenderMeshSystemV2
					,	typeof(UpdatePresentationSystemGroup)
					,	typeof(PresentationSystemGroup)
					,	typeof(StructuralChangePresentationSystemGroup)

					// fixes: "Internal: deleting an allocation that is older than its permitted lifetime of 4 frames (age = 15)"
					,	typeof(EndSimulationEntityCommandBufferSystem)

					,	typeof(SegmentInitializationSystem)
					,	typeof(SegmentTransformSystem)
					,	typeof(SegmentWorldBoundsSystem)
					,	typeof(CreateSegmentsSystem)
				};
				DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups( world , systems );
				ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop( world );
				
				// var defaultSystems = DefaultWorldInitialization.GetAllSystems( WorldSystemFilterFlags.Default );
				// DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups( world , defaultSystems );
				// // EntitySceneOptimization.Optimize( world );// what's this for?
				// ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop( world );
			}
			return world;
		}


		public static void InstantiatePool ( int length , out NativeArray<Entity> entities ) => InstantiatePool( length , out entities , Prototypes.k_defaul_segment_width , Internal.ResourceProvider.default_material );
		public static void InstantiatePool ( int length , out NativeArray<Entity> entities , float width ) => InstantiatePool( length , out entities , width , Internal.ResourceProvider.default_material );
		public static void InstantiatePool ( int length , out NativeArray<Entity> entities , Material material ) => InstantiatePool( length , out entities , Prototypes.k_defaul_segment_width , material );
		public static void InstantiatePool ( int length , out NativeArray<Entity> entities , float width , Material material )
		{
			GetOrCreateWorld();// make sure world exists
			var command = GetEntityManager();
			var prefab = GetSegmentPrefabCopy();
			{
				if( material!=null )
				{
					var renderMesh = command.GetSharedComponentData<RenderMesh>( prefab );
					if( renderMesh.material!=material )
					{
						renderMesh.material = material;
						command.SetSharedComponentData( prefab , renderMesh );
					}
				}
				else Debug.LogError($"material is null");

				command.SetComponentData( prefab , new SegmentWidth{ Value = (half)width } );
			}
			entities = command.Instantiate( srcEntity:prefab , instanceCount:length , allocator:Allocator.Temp );
			command.DestroyEntity( prefab );
		}
		public static void InstantiatePool ( int length , out Entity[] entities ) => InstantiatePool( length , out entities , Prototypes.k_defaul_segment_width , Internal.ResourceProvider.default_material );
		public static void InstantiatePool ( int length , out Entity[] entities , float width ) => InstantiatePool( length , out entities , width , Internal.ResourceProvider.default_material );
		public static void InstantiatePool ( int length , out Entity[] entities , Material material ) => InstantiatePool( length , out entities , Prototypes.k_defaul_segment_width , material );
		public static void InstantiatePool ( int length , out Entity[] entities , float width , Material material )
		{
			InstantiatePool( length , out NativeArray<Entity> instances , width , material );
			entities = instances.ToArray();
			instances.Dispose();
		}
		public static void InstantiatePool ( Entity[] entities ) => InstantiatePool( entities , Prototypes.k_defaul_segment_width , Internal.ResourceProvider.default_material );
		public static void InstantiatePool ( Entity[] entities , float width ) => InstantiatePool( entities , width , Internal.ResourceProvider.default_material );
		public static void InstantiatePool ( Entity[] entities , Material material ) => InstantiatePool( entities , Prototypes.k_defaul_segment_width , material );
		public static void InstantiatePool ( Entity[] entities , float width , Material material )
		{
			InstantiatePool( entities.Length , out NativeArray<Entity> instances , width , material );
			entities = instances.ToArray();
			instances.Dispose();
		}

		/// Upsizes pool length when it's < minLength
		public static void Upsize ( NativeArray<Entity> entities , int minLength )
		{
			Assert.IsTrue( entities.IsCreated );
			int length = entities.Length;
			if( length < minLength )
			{
				int difference = minLength - length;
				#if UNITY_EDITOR
				Debug.Log($"↑ upsizing pool (length) {length} < {minLength} (minLength)");
				#endif

				var command = GetEntityManager();
				NativeArray<Entity> newEntities;
				if( entities!=null && length!=0 )
				{
					var prefab = entities[0];
					newEntities = command.Instantiate( srcEntity:prefab , instanceCount:difference , allocator:Allocator.Temp );
				}
				else
				{
					var prefab = GetSegmentPrefabCopy();
					newEntities = command.Instantiate( srcEntity:prefab , instanceCount:difference , allocator:Allocator.Temp );
					command.DestroyEntity( prefab );
				}
				var resizedEntities = new NativeArray<Entity>( minLength , Allocator.Persistent , NativeArrayOptions.UninitializedMemory );
				NativeArray<Entity>.Copy( src:entities , dst:resizedEntities );
				NativeArray<Entity>.Copy(
					src:		newEntities ,
					srcIndex:	0 ,
					dst:		resizedEntities ,
					dstIndex:	length ,
					length:		newEntities.Length
				);
				entities.Dispose();
				newEntities.Dispose();
				entities = resizedEntities;
			}
		}
		/// Upsizes pool length when it's < minLength
		public static void Upsize ( ref Entity[] entities , int minLength )
		{
			Assert.IsNotNull( entities );
			int length = entities.Length;
			if( length < minLength )
			{
				int difference = minLength - length;
				#if UNITY_EDITOR
				Debug.Log($"↑ upsizing pool (length) {length} < {minLength} (minLength)");
				#endif

				var command = GetEntityManager();
				NativeArray<Entity> newEntities;
				if( entities!=null && length!=0 )
				{
					var prefab = entities[0];
					newEntities = command.Instantiate( srcEntity:prefab , instanceCount:difference , allocator:Allocator.Temp );
				}
				else
				{
					var prefab = GetSegmentPrefabCopy();
					newEntities = command.Instantiate( srcEntity:prefab , instanceCount:difference , allocator:Allocator.Temp );
					command.DestroyEntity( prefab );
				}
				Entity[] newEntitiesArray = newEntities.ToArray();
				newEntities.Dispose();
				System.Array.Resize( ref entities , minLength );
				System.Array.Copy(
					sourceArray:		newEntitiesArray ,
					sourceIndex:		0 ,
					destinationArray:	entities ,
					destinationIndex:	length ,
					length:				newEntitiesArray.Length
				);
			}
		}

		/// Downsizes pool length when it's > maxLength
		public static void Downsize ( ref Entity[] entities , int maxLength )
		{
			Assert.IsNotNull( entities , $"{nameof(entities)} array is null" );
			int length = entities.Length;
			if( length > maxLength )
			{
				#if UNITY_EDITOR
				Debug.Log($"↓ downsizing pool (length) {length} > {maxLength} (maxLength)");
				#endif

				var command = GetEntityManager();
				for( int i=maxLength ; i<length ; i++  )
					command.DestroyEntity( entities[i] );

				System.Array.Resize( ref entities , maxLength );
			}
		}

		public static void DestroyEntities ( Entity[] entities )
		{
			Assert.IsNotNull( entities );
			int length = entities.Length;
			var command = GetEntityManager();
			for( int i=0 ; i<length ; i++  )
				command.DestroyEntity( entities[i] );
		}


	}
}
