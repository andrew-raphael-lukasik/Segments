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

        static EntityQuery _query;
        public static EntityQuery Query => _query;

        static Material _default_material;
        internal static World _world;

        internal static World GetWorld ()
        {
            if( _world!=null && _world.IsCreated )
                return _world;
            else
            {
                _world = World.DefaultGameObjectInjectionWorld;
                
                #if UNITY_EDITOR
                if( _world==null )
                {
                    // create editor world:
                    _world = DefaultWorldInitialization.Initialize( "Editor World" , true );
                    // DefaultWorldInitialization.DefaultLazyEditModeInitialize();// not immediate
                }
                #endif

                _query = _world.EntityManager.CreateEntityQuery( typeof(Segment) );

                if( _default_material==null )
                {
                    const string path = "packages/com.andrewraphaellukasik.segments/default";
                    _default_material = Resources.Load<Material>( path );
                    if( _default_material!=null )
                        _default_material.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    else
                        Debug.LogWarning($"loading Material asset failed, path: \'{path}\'");
                }

                return _world;
            }
        }

        public static void Create ( out Entity entity , Material material = null )
        {
            var entityManager = GetWorld().EntityManager;
            Create( entityManager , out entity , material );
        }
        public static void Create ( out Entity entity , out EntityManager entityManager , Material material = null )
        {
            entityManager = GetWorld().EntityManager;
            Create( entityManager , out entity , material );
        }
        public static void Create ( EntityManager entityManager , out Entity entity , Material material = null )
        {
            _query.CompleteDependency();
            
            entity = entityManager.CreateEntity( typeof(Segment) );
            if( material==null )
            {
                if( _default_material==null )
                {
                    const string path = "packages/com.andrewraphaellukasik.segments/default";
                    _default_material = Resources.Load<Material>( path );
                    if( _default_material!=null )
                        _default_material.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    else
                        Debug.LogWarning($"loading Material asset failed, path: \'{path}\'");
                }
                
                material = _default_material;
            }
            entityManager.AddSharedComponentManaged( entity , new SegmentCreationRequestData{
                material = material
            } );
            
            entityManager.AddComponentData( entity , new LocalToWorld{
                Value = float4x4.identity
            } );
        }

        public static void DestroyAll ()
        {
            if( _world.IsCreated )
            {
                _query.CompleteDependency();
                _world.EntityManager.DestroyEntity( _query );
            }
        }

        public static void Destroy ( Entity entity )
        {
            if( _world.IsCreated )
            {
                _query.CompleteDependency();
                _world.EntityManager.DestroyEntity( entity );
            }
        }
        /// <summary> Can be called from a Burst-compiled ISystem </summary>
        public static void Destroy ( Entity entity , EntityManager entityManager )
        {
            entityManager.CreateEntityQuery( new EntityQueryBuilder(Allocator.Temp).WithAll<Segment>() ).CompleteDependency();
            entityManager.DestroyEntity( entity );
        }

        /// <summary> Pass jobhandle when scheduling a job, that accesses a segment buffer, from where ECS can't track it automatically (Monobehaviours) </summary>
        public static void AddDependency ( JobHandle dependency ) => _query.AddDependency( dependency );

        /// <summary> Returns a Segment buffer reintepreted as float3x2 for convenience </summary>
        public static DynamicBuffer<float3x2> GetBuffer ( Entity entity , bool isReadOnly = false ) => _world.EntityManager.GetBuffer<Segment>( entity , isReadOnly ).Reinterpret<float3x2>();

    }
}
