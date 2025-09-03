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

        internal static Material _default_material;
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
                    const string path = "packages/com.andrewraphaellukasik.segments/segments--default-line-material";
                    _default_material = Resources.Load<Material>( path );
                    if( _default_material!=null )
                        _default_material.hideFlags = HideFlags.DontUnloadUnusedAsset;
                    else
                        Debug.LogWarning($"loading Material asset failed, path: \'{path}\'");
                }

                return _world;
            }
        }

        /// <summary> Creates a Segment </summary>
        /// <remarks> Can be called from outside ECS (MonoBehaviour etc.) </remarks>
        public static void Create ( out Entity entity , Material material = null )
        {
            var entityManager = GetWorld().EntityManager;
            Create( entityManager , out entity , material );
        }
        /// <summary> Creates a Segment </summary>
        /// <remarks> Can be called from outside ECS (MonoBehaviour etc.) </remarks>
        public static void Create ( out Entity entity , out EntityManager entityManager , Material material = null )
        {
            entityManager = GetWorld().EntityManager;
            Create( entityManager , out entity , material );
        }
        /// <summary> Creates a Segment </summary>
        /// <remarks> Can be called from outside ECS (MonoBehaviour etc.) </remarks>
        public static void Create ( EntityManager entityManager , out Entity entity , Material material = null )
        {
            _query.CompleteDependency();
            
            entity = entityManager.CreateEntity(typeof(SegmentsInitializationRequest), typeof(Segment), typeof(LocalToWorld));
            
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
            entityManager.AddSharedComponentManaged( entity , new SegmentsInitializationRequest{
                material = material
            } );

            entityManager.AddComponentData( entity , new Segment{
                Buffer = new NativeList<float3x2>(Allocator.Persistent),
                Dependency = new NativeReference<JobHandle>(Allocator.Persistent),
            } );
            
            entityManager.AddComponentData( entity , new LocalToWorld{
                Value = float4x4.identity
            } );
        }

        /// <summary> Destroys an entity and it's all Segment data </summary>
        /// <remarks> Can be called from outside ECS (MonoBehaviour etc.) </remarks>
        public static void Destroy ( Entity entity )
        {
            if( _world.IsCreated )
            {
                var em = _world.EntityManager;
                if( em.HasComponent<Segment>(entity) )
                {
                    Segment seg = em.GetComponentData<Segment>(entity);
                    seg.Dependency.Value.Complete();
                    seg.Buffer.Dispose();
                }

                _world.EntityManager.DestroyEntity(entity);
            }
        }
        /// <summary> Destroys an entity and it's all Segment data </summary>
        /// <remarks> Can be called from a Burst-compiled code block </remarks>
        public static void Destroy ( Entity entity , EntityManager entityManager )
        {
            if( entityManager.HasComponent<Segment>(entity) )
            {
                Segment seg = entityManager.GetComponentData<Segment>(entity);
                seg.Dependency.Value.Complete();
                seg.Buffer.Dispose();
            }
            entityManager.DestroyEntity(entity);
        }

        /// <summary> Destroys all Segment entities and their data </summary>
        /// <remarks> Can be called from outside ECS (MonoBehaviour etc.) </remarks>
        public static void DestroyAll ()
        {
            if( _world.IsCreated )
            {
                _query.CompleteDependency();

                var em = _world.EntityManager;
                foreach( Entity e in _query.ToEntityArray(Allocator.Temp) )
                if( em.HasComponent<Segment>(e) )
                {
                    Segment seg = em.GetComponentData<Segment>(e);
                    seg.Dependency.Value.Complete();
                    seg.Buffer.Dispose();
                }

                _world.EntityManager.DestroyEntity(_query);
            }
        }
        /// <summary> Destroys all Segment entities and their data </summary>
        /// <remarks> Can be called from a Burst-compiled code block </remarks>
        public static void DestroyAll ( EntityManager entityManager )
        {
            var query = new EntityQueryBuilder().WithAll<Segment>().Build(entityManager);
            entityManager.CreateEntityQuery(ComponentType.ReadOnly<Segment>());
            foreach( Entity e in query.ToEntityArray(Allocator.Temp) )
            if( entityManager.HasComponent<Segment>(e) )
            {
                Segment seg = entityManager.GetComponentData<Segment>(e);
                seg.Dependency.Value.Complete();
                seg.Buffer.Dispose();
            }

            _world.EntityManager.DestroyEntity(query);
        }

        /// <summary> Gets you Segment component data </summary>
        /// <remarks> Can be called from outside ECS (MonoBehaviour etc.) </remarks>
        public static Segment GetSegment ( Entity entity ) => _world.EntityManager.GetComponentData<Segment>(entity);
        /// <summary> Gets you Segment component data </summary>
        /// <remarks> Can be called from a Burst-compiled code block </remarks>
        public static Segment GetSegment ( Entity entity , EntityManager entityManager ) => entityManager.GetComponentData<Segment>(entity);

        /// <summary> Enables the SegmentUpdateRequest component to trigger AABB recalculation and buffer be copied to the GPU again </summary>
        /// <remarks> Can be called from outside ECS (MonoBehaviour etc.) </remarks>
        public static void SetSegmentChanged ( Entity entity ) => _world.EntityManager.SetComponentEnabled<SegmentUpdateRequest>(entity, true);
        /// <summary> Enables the SegmentUpdateRequest component to trigger AABB recalculation and buffer be copied to the GPU again </summary>
        /// <remarks> Can be called from a Burst-compiled code block </remarks>
        public static void SetSegmentChanged ( Entity entity , EntityManager entityManager ) => entityManager.SetComponentEnabled<SegmentUpdateRequest>(entity, true);

    }
}
