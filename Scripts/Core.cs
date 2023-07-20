using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;
using Unity.Mathematics;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Rendering;
using Unity.Transforms;

namespace Segments
{
	public static class Core
	{

		public static EntityQuery Query { get; private set; }

		static Material default_material;
		internal static World world;

		internal static World GetWorld ()
		{
			if( world!=null && world.IsCreated )
				return world;
			else
			{
				world = World.DefaultGameObjectInjectionWorld;
				
				#if UNITY_EDITOR
				if( world==null )
				{
					// create editor world:
					world = DefaultWorldInitialization.Initialize( "Editor World" , true );
					// DefaultWorldInitialization.DefaultLazyEditModeInitialize();// not immediate
				}
				#endif

				Query = world.EntityManager.CreateEntityQuery( typeof(Segment) );

				if( default_material==null )
				{
					const string path = "packages/Segments/default";
					default_material = UnityEngine.Resources.Load<Material>( path );
					if( default_material!=null )
						default_material.hideFlags = HideFlags.DontUnloadUnusedAsset;
					else
						Debug.LogWarning($"loading Material asset failed, path: \'{path}\'");
				}

				//DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups( Entities.world , typeof(SegmentInitializationSystem) , typeof(SegmentRenderingSystem) );

				return world;
			}
		}

		public static void CreateBatch ( out Entity entity , Material material = null )
		{
			var entityManager = GetWorld().EntityManager;
			CreateBatch( entityManager , out entity , material );
		}
		public static void CreateBatch ( out Entity entity , out EntityManager entityManager , Material material = null )
		{
			entityManager = GetWorld().EntityManager;
			CreateBatch( entityManager , out entity , material );
		}
		public static void CreateBatch ( EntityManager entityManager , out Entity entity , Material material = null )
		{
			Query.CompleteDependency();

			entity = entityManager.CreateEntity( typeof(Segment) );
			if( material==null ) material = default_material;

            if( default_material==null )
			{
				const string path = "packages/Segments/default";
				default_material = UnityEngine.Resources.Load<Material>( path );
				if( default_material!=null )
					default_material.hideFlags = HideFlags.DontUnloadUnusedAsset;
				else
					Debug.LogWarning($"loading Material asset failed, path: \'{path}\'");
			}
			
			var mesh = new Mesh();
            mesh.name = $"Segments mesh {entity}";
            mesh.MarkDynamic();
                
            var renderMeshDescription = new RenderMeshDescription( shadowCastingMode:ShadowCastingMode.On , receiveShadows:true , renderingLayerMask:1 );
            var meshArray = new RenderMeshArray(
                new[]{ material ?? default_material } ,
                new[]{ mesh }
            );
            var materialMeshInfo = MaterialMeshInfo.FromRenderMeshArrayIndices(0,0);
            RenderMeshUtility.AddComponents( entity , entityManager , renderMeshDescription , meshArray , materialMeshInfo );

			entityManager.SetComponentData( entity , new LocalToWorld{
				Value = float4x4.identity
			} );
                
            entityManager.SetName( entity , mesh.name );
		}

		public static void DestroyAllBatches ()
		{
			var entityManager = world.EntityManager;
			var query = entityManager.CreateEntityQuery( ComponentType.ReadOnly<Segment>() );
			entityManager.DestroyEntity( query );
		}

		public static void DestroyBatch ( Entity entity )
		{
			var entityManager = world.EntityManager;
			Query.CompleteDependency();
			entityManager.DestroyEntity( entity );
		}

		public static void CompleteDependency ()
			=> Query.CompleteDependency();

		public static void AddDependency ( JobHandle dependency )
		{
			JobHandle combined = JobHandle.CombineDependencies( dependency , Query.GetDependency() );
			Query.AddDependency( combined );
		}

		public static JobHandle GetDependency ()
		{
			return Query.GetDependency();
		}

		public static DynamicBuffer<float3x2> GetSegmentBuffer ( Entity entity , bool isReadOnly = false )
		{
			var entityManager = world.EntityManager;
			return entityManager.GetBuffer<Segment>( entity , isReadOnly ).Reinterpret<float3x2>();
		}

	}
	
	public struct Segment : IBufferElementData
	{
		public float3x2 Value;
	}

}
