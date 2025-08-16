using UnityEngine;
using UnityEngine.Rendering;
using Unity.Profiling;
using Unity.Entities;
using Unity.Jobs;
using Unity.Collections;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEditor;

namespace Segments
{
    [WorldSystemFilter( WorldSystemFilterFlags.Presentation | WorldSystemFilterFlags.Editor )]
    [UpdateInGroup( typeof(InitializationSystemGroup) )]
    [RequireMatchingQueriesForUpdate]
    [Unity.Burst.BurstCompile]
    partial struct SegmentInitializationSystem : ISystem
    {
        EntityQuery _query;

        [Unity.Burst.BurstCompile]
        public void OnCreate ( ref SystemState state )
        {
            _query = new EntityQueryBuilder(Allocator.Temp).WithAll<SegmentCreationRequestData>().Build( ref state );
            state.RequireForUpdate( _query );
        }

        // [Unity.Burst.BurstCompile]
        public void OnUpdate ( ref SystemState state )
        {
            var entitiesGraphicsSystem = state.World.GetExistingSystemManaged<EntitiesGraphicsSystem>();
            var entityManager = state.EntityManager;
            
            foreach( Entity entity in _query.ToEntityArray(Allocator.Temp) )
            {
                // replace request with components:
                {
                    var mesh = new Mesh();
                    string label = $"Segments mesh {mesh.GetHashCode()}";
                    mesh.name = label;
                    mesh.MarkDynamic();
                    mesh.hideFlags = HideFlags.DontSave;

                    var data = entityManager.GetSharedComponentManaged<SegmentCreationRequestData>( entity );
                    Material mat = data.material!=null ? data.material : Core._default_material;
                    BatchMaterialID batchMaterialID = entitiesGraphicsSystem.RegisterMaterial( mat );
                    var renderMeshDescription = new RenderMeshDescription( shadowCastingMode:ShadowCastingMode.On , receiveShadows:true , renderingLayerMask:1 );
                    BatchMeshID batchMeshID = entitiesGraphicsSystem.RegisterMesh( mesh );
                    var materialMeshInfo = new MaterialMeshInfo( batchMaterialID , batchMeshID );
                    RenderMeshUtility.AddComponents( entity , entityManager , renderMeshDescription , materialMeshInfo );

                    #if UNITY_EDITOR
                    entityManager.SetName( entity , label );
                    #endif
                }
                entityManager.RemoveComponent<SegmentCreationRequestData>( entity );

                // add LTW if not added already:
                if( !entityManager.HasComponent<LocalToWorld>(entity) )
                {
                    entityManager.AddComponentData( entity , new LocalToWorld{
                        Value = float4x4.identity
                    } );
                }
            }
        }
    }

    struct SegmentCreationRequestData : ISharedComponentData, System.IEquatable<SegmentCreationRequestData>
    {
        public Material material;

        public bool Equals ( SegmentCreationRequestData other )
        {
            if( other.material==null ) return false;
            return this.material==other.material;
        }
        public override int GetHashCode ()
        {
            int hash = base.GetHashCode();
            if( this.material!=null ) hash += 97 * this.material.GetHashCode();
            return hash;
        }
    }

}
